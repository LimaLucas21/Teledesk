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

        /// <summary>
        /// Endpoint GET de verificação caso seja utilizado também com a Meta Cloud API.
        /// </summary>
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

        /// <summary>
        /// Recebe eventos do WAHA (https://waha.devlike.pro/swagger/) ou da Meta Cloud API.
        /// </summary>
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
                    var wahaPayload = JsonSerializer.Deserialize<WahaWebhookPayload>(rawBody);
                    if (wahaPayload != null)
                    {
                        await _conversationService.ProcessWahaWebhookAsync(wahaPayload);
                    }
                    return Ok();
                }

                if (root.TryGetProperty("entry", out _))
                {
                    var metaPayload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(rawBody);
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

        /// <summary>
        /// Endpoint auxiliar para simular o recebimento de uma mensagem WhatsApp durante testes locais.
        /// </summary>
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
