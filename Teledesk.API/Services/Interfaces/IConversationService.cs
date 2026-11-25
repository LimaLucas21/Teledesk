using Teledesk.API.DTOs;

namespace Teledesk.API.Services.Interfaces
{
    public class ResultadoProcessamentoWhatsApp
    {
        public bool Sucesso { get; set; }
        public int? AtendimentoId { get; set; }
        public int? SetorId { get; set; }
        public string? SetorNome { get; set; }
        public string? RespostaAutomatica { get; set; }
        public string Observacao { get; set; } = string.Empty;
    }

    public interface IConversationService
    {
        Task ProcessWebhookPayloadAsync(WhatsAppWebhookPayload payload);
        Task<ResultadoProcessamentoWhatsApp> ProcessIncomingWhatsAppMessageAsync(
            string phoneNumberId,
            string waId,
            string nomeCliente,
            string texto,
            string? whatsAppMessageId = null,
            string? displayPhoneNumber = null);
    }
}
