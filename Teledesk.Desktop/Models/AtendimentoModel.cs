namespace Teledesk.Desktop.Models
{
    public class AtendimentoModel
    {
        public int Id { get; set; }
        public int ContatoId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string? UsernameCliente { get; set; }
        public long TelegramChatId { get; set; }
        public int Status { get; set; }
        public int? AtendenteId { get; set; }
        public string? AtendenteNome { get; set; }
        public DateTime DataInicio { get; set; }
        public string? UltimaMensagem { get; set; }

        public override string ToString()
        {
            var statusTexto = Status == 1 ? "⏳ [Fila]" : "💬 [Em Atendimento]";
            var user = !string.IsNullOrEmpty(UsernameCliente) ? $" (@{UsernameCliente})" : "";
            return $"{statusTexto} #{Id} - {NomeCliente}{user}";
        }
    }
}
