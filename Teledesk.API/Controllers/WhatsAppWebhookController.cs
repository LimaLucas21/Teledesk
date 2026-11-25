using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Teledesk.API.DTOs;
using Teledesk.API.Services.Interfaces;

namespace Teledesk.API.Controllers
{
    [ApiController]
    [Route("api/webhook/whatsapp")]
    public class WhatsAppWebhookController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IConversationService _conversationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppWebhookController> _logger;

        public WhatsAppWebhookController(
            IConversationService conversationService,
            IConfiguration configuration,
            ILogger<WhatsAppWebhookController> logger)
        {
            _conversationService = conversationService;
            _configuration = configuration;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerificarWebhook(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            var expectedToken = _configuration["WhatsAppSettings:VerifyToken"] ?? "teledesk_verify_token";

            if (mode == "subscribe" && verifyToken == expectedToken && !string.IsNullOrEmpty(challenge))
            {
                _logger.LogInformation("Webhook verificado com sucesso!");
                return Content(challenge, "text/plain");
            }

            return Forbid();
        }

        [AllowAnonymous]
        [HttpPost]
        [HttpPost("waha")]
        public async Task<IActionResult> ReceberEvento()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(rawBody))
                return Ok();

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("event", out _))
                {
                    var wahaPayload = JsonSerializer.Deserialize<WahaWebhookPayload>(rawBody, JsonOptions);
                    if (wahaPayload != null)
                    {
                        _logger.LogInformation(
                            "📥 Webhook WAHA recebido: event={Event}, session={Session}, from={From}, fromMe={FromMe}, alt={Alt}",
                            wahaPayload.Event,
                            wahaPayload.Session,
                            wahaPayload.Payload?.From,
                            wahaPayload.Payload?.FromMe,
                            wahaPayload.Payload?.Data?.Key?.RemoteJidAlt ?? wahaPayload.Payload?.Data?.Key?.SenderPn);

                        await _conversationService.ProcessWahaWebhookAsync(wahaPayload);
                    }
                    return Ok();
                }

                if (root.TryGetProperty("entry", out _))
                {
                    var metaPayload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(rawBody, JsonOptions);
                    if (metaPayload != null)
                    {
                        await _conversationService.ProcessWebhookPayloadAsync(metaPayload);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento do webhook do WhatsApp/WAHA.");
                return Ok();
            }
        }

        [AllowAnonymous]
        [HttpPost("simular")]
        public async Task<IActionResult> SimularMensagem([FromBody] SimularMensagemWhatsAppDto dto)
        {
            var resultado = await _conversationService.ProcessIncomingWhatsAppMessageAsync(
                dto.PhoneNumberId,
                dto.WaId,
                dto.NomeCliente,
                dto.Texto,
                $"sim_{Guid.NewGuid():N}");

            return Ok(resultado);
        }
    }
}
