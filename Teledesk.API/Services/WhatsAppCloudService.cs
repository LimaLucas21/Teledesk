using System.Net.Http.Json;
using System.Text.Json;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Services
{
    public class WhatsAppCloudService : IWhatsAppService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppCloudService> _logger;

        public WhatsAppCloudService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<WhatsAppCloudService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> EnviarMensagemTextoAsync(
            string phoneNumberId,
            string destinatarioWaId,
            string texto,
            string? customAccessToken = null,
            CancellationToken cancellationToken = default)
        {
            var wahaBaseUrl = (_configuration["WhatsAppSettings:WahaBaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var defaultSession = _configuration["WhatsAppSettings:DefaultSession"] ?? "default";
            var session = string.IsNullOrWhiteSpace(phoneNumberId) || phoneNumberId == "100000000000000"
                ? defaultSession
                : phoneNumberId.Trim();

            var apiKey = !string.IsNullOrWhiteSpace(customAccessToken)
                ? customAccessToken
                : _configuration["WhatsAppSettings:WahaApiKey"];

            // Preserva @lid ou @c.us quando já presente; adiciona @c.us apenas para números puros
            var chatId = FormatarChatIdParaWaha(destinatarioWaId);

            try
            {
                var client = _httpClientFactory.CreateClient("WhatsAppCloud");
                var url = $"{wahaBaseUrl}/api/sendText";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("X-Api-Key", apiKey);
                }

                request.Content = JsonContent.Create(new
                {
                    session,
                    chatId,
                    text = texto
                });

                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var erro = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Falha ao enviar mensagem pelo WAHA ({StatusCode}) para '{ChatId}' na sessão '{Session}': {Erro}",
                        response.StatusCode, chatId, session, erro);
                    return false;
                }

                _logger.LogInformation(
                    "📲 [WAHA Enviado | Sessão: {Session} -> {ChatId}]: {Texto}",
                    session, chatId, texto);
                return true;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    "WAHA inacessível em {Url} ({Erro}). [Simulado | Sessão: {Session} -> {ChatId}]: {Texto}",
                    wahaBaseUrl, ex.Message, session, chatId, texto);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar mensagem para o WAHA.");
                return false;
            }
        }

        public async Task<string?> ResolverLidParaTelefoneAsync(
            string session,
            string lid,
            string? customAccessToken = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(lid))
                return null;

            var wahaBaseUrl = (_configuration["WhatsAppSettings:WahaBaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
            var apiKey = !string.IsNullOrWhiteSpace(customAccessToken)
                ? customAccessToken
                : _configuration["WhatsAppSettings:WahaApiKey"];

            try
            {
                var client = _httpClientFactory.CreateClient("WhatsAppCloud");
                var url = $"{wahaBaseUrl}/api/{Uri.EscapeDataString(session)}/lids/{Uri.EscapeDataString(lid)}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("X-Api-Key", apiKey);
                }

                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("pn", out var pnProp))
                {
                    var pn = pnProp.GetString();
                    if (!string.IsNullOrWhiteSpace(pn))
                    {
                        var idx = pn.IndexOf('@');
                        return idx > 0 ? pn[..idx] : pn;
                    }
                }
            }
            catch
            {
                // Se o store de LIDs não estiver ativo no WAHA, segue com o próprio @lid
            }

            return null;
        }

        private static string FormatarChatIdParaWaha(string destinatarioWaId)
        {
            var limpo = destinatarioWaId.Trim();
            if (limpo.EndsWith("@s.whatsapp.net", StringComparison.OrdinalIgnoreCase))
            {
                return limpo[..limpo.IndexOf('@')] + "@c.us";
            }

            if (limpo.Contains('@'))
            {
                return limpo;
            }

            return $"{limpo.TrimStart('+')}@c.us";
        }
    }
}
