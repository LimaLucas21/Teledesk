using System.Security.Cryptography;
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
        /// Endpoint oficial GET exigido pela Meta para verificar o Webhook da WhatsApp Cloud API.
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
                _logger.LogInformation("Webhook da WhatsApp Cloud API verificado com sucesso!");
                return Content(challenge, "text/plain");
            }

            _logger.LogWarning("Falha na verificação do Webhook do WhatsApp (VerifyToken inválido).");
            return Forbid();
        }

        /// <summary>
        /// Endpoint oficial POST que recebe os eventos de mensagens da Meta (WhatsApp Cloud API).
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ReceberEvento()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();

            var appSecret = _configuration["WhatsAppSettings:AppSecret"];
            if (!string.IsNullOrWhiteSpace(appSecret))
            {
                var signatureHeader = Request.Headers["X-Hub-Signature-256"].ToString();
                if (!ValidarAssinaturaMeta(rawBody, signatureHeader, appSecret))
                {
                    _logger.LogWarning("Assinatura X-Hub-Signature-256 inválida no webhook do WhatsApp.");
                    return Unauthorized();
                }
            }

            try
            {
                var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(rawBody);
                if (payload != null)
                {
                    await _conversationService.ProcessWebhookPayloadAsync(payload);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento do webhook do WhatsApp.");
                // Retorna 200 OK para evitar loop de retentativas da Meta
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

        private static bool ValidarAssinaturaMeta(string rawBody, string signatureHeader, string appSecret)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader) ||
                !signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var hexRecebido = signatureHeader["sha256=".Length..];
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
            var hexEsperado = Convert.ToHexString(hash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hexEsperado),
                Encoding.UTF8.GetBytes(hexRecebido.ToLowerInvariant()));
        }
    }
}
