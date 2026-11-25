using System.Net.Http.Headers;
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
            var accessToken = !string.IsNullOrWhiteSpace(customAccessToken)
                ? customAccessToken
                : _configuration["WhatsAppSettings:DefaultAccessToken"];

            var apiVersion = _configuration["WhatsAppSettings:ApiVersion"] ?? "v21.0";

            // Modo simulação local caso o token da Meta ainda não esteja configurado
            if (string.IsNullOrWhiteSpace(accessToken) || phoneNumberId == "100000000000000")
            {
                _logger.LogInformation(
                    "📲 [WhatsApp Simulado | PhoneNumberId: {PhoneNumberId} -> {WaId}]: {Texto}",
                    phoneNumberId, destinatarioWaId, texto);
                return true;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("WhatsAppCloud");
                var url = $"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(new
                {
                    messaging_product = "whatsapp",
                    recipient_type = "individual",
                    to = destinatarioWaId,
                    type = "text",
                    text = new
                    {
                        preview_url = false,
                        body = texto
                    }
                });

                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var erro = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Falha ao enviar mensagem na WhatsApp Cloud API ({StatusCode}): {Erro}",
                        response.StatusCode, erro);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro de comunicação com a WhatsApp Cloud API.");
                return false;
            }
        }
    }
}
