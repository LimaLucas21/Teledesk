using Teledesk.API.Models.Enums;

namespace Teledesk.API.Models
{
    public class Mensagem
    {
        public int Id { get; set; }

        // FK e Propriedade de Navegação para o Atendimento
        public int AtendimentoId { get; set; }
        public Atendimento Atendimento { get; set; } = null!;

        public string Texto { get; set; } = string.Empty;
        public OrigemMensagem Origem { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    }
}
