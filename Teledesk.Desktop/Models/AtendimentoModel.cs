namespace Teledesk.Desktop.Models
{
    public class AtendimentoModel
    {
        public int Id { get; set; }
        public int ContatoId { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string? UsernameCliente { get; set; }
        public string WhatsAppId { get; set; } = string.Empty;
        public int? SetorId { get; set; }
        public string? SetorNome { get; set; }
        public int? NumeroWhatsAppId { get; set; }
        public string? NumeroExibicao { get; set; }
        public int Status { get; set; }
        public int? AtendenteId { get; set; }
        public string? AtendenteNome { get; set; }
        public DateTime DataInicio { get; set; }
        public string? UltimaMensagem { get; set; }

        public override string ToString()
        {
            var statusTexto = Status switch
            {
                0 => "🤖 [Em URA]",
                1 => "⏳ [Fila]",
                _ => "💬 [Em Atendimento]"
            };
            var setor = !string.IsNullOrWhiteSpace(SetorNome) && Status != 0 ? $" [{SetorNome}]" : "";
            return $"{statusTexto}{setor} #{Id} - {NomeCliente} ({WhatsAppId})";
        }
    }
}
