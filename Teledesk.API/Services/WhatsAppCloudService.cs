using System.Net.Http.Json;
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

            var chatId = destinatarioWaId.Contains('@')
                ? destinatarioWaId.Trim()
                : $"{destinatarioWaId.Trim().TrimStart('+')}@c.us";

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
                        "Falha ao enviar mensagem pelo WAHA ({StatusCode}) na sessão '{Session}': {Erro}",
                        response.StatusCode, session, erro);
                    return false;
                }

                _logger.LogInformation(
                    "📲 [WAHA Enviado | Sessão: {Session} -> {ChatId}]: {Texto}",
                    session, chatId, texto);
                return true;
            }
            catch (HttpRequestException ex)
            {
                // Permite usar o Simulador Desktop mesmo quando o container WAHA não estiver em execução
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
    }
}
