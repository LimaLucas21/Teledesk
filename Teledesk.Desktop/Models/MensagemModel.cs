namespace Teledesk.Desktop.Models
{
    public class MensagemModel
    {
        public int Id { get; set; }
        public int AtendimentoId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public int Origem { get; set; } // 0 = Cliente, 1 = Atendente, 2 = Sistema
        public DateTime DataEnvio { get; set; }

        public string FormatarParaExibicao()
        {
            var hora = DataEnvio.ToLocalTime().ToString("HH:mm");
            return Origem switch
            {
                0 => $"[{hora}] 👤 Cliente: {Texto}",
                1 => $"[{hora}] 🧑‍💻 Você: {Texto}",
                _ => $"[{hora}] 🤖 Sistema: {Texto}"
            };
        }
    }
}
