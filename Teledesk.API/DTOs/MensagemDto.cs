using Teledesk.API.Models.Enums;

namespace Teledesk.API.DTOs
{
    public class MensagemDto
    {
        public int Id { get; set; }
        public int AtendimentoId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public OrigemMensagem Origem { get; set; }
        public DateTime DataEnvio { get; set; }
    }
}
