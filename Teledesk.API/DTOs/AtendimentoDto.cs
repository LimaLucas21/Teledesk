using Teledesk.API.Models.Enums;

namespace Teledesk.API.DTOs
{
    public class AtendimentoDto
    {
        public int Id { get; set; }
        public int ContatoId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string? UsernameCliente { get; set; }
        public long TelegramChatId { get; set; }
        public StatusAtendimento Status { get; set; }
        public int? AtendenteId { get; set; }
        public string? AtendenteNome { get; set; }
        public DateTime DataInicio { get; set; }
        public string? UltimaMensagem { get; set; }
    }
}
