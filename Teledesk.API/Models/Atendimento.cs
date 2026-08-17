using Teledesk.API.Models.Enums;

namespace Teledesk.API.Models
{
    public class Atendimento
    {
        public int Id { get; set; }

        // FK e Propriedade de Navegação para o Contato (Obrigatório)
        public int ContatoId { get; set; }
        public Contato Contato { get; set; } = null!;

        // FK e Propriedade de Navegação para o Atendente (Opcional - pode ser nulo se estiver na URA)
        public int? AtendenteId { get; set; }
        public Atendente? Atendente { get; set; }

        public StatusAtendimento Status { get; set; } = StatusAtendimento.EmUra;
        public DateTime DataInicio { get; set; } = DateTime.UtcNow;
        public DateTime? DataFim { get; set; }

        // Propriedade de navegação (Um atendimento possui um histórico de mensagens)
        public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    }
}
