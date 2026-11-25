using Teledesk.API.Models.Enums;

namespace Teledesk.API.Models
{
    public class Atendimento
    {
        public int Id { get; set; }

        // FK e Propriedade de Navegação para o Contato (Obrigatório)
        public int ContatoId { get; set; }
        public Contato Contato { get; set; } = null!;

        // FK e Propriedade de Navegação para o Atendente (Opcional - nulo enquanto na URA ou na fila)
        public int? AtendenteId { get; set; }
        public Atendente? Atendente { get; set; }

        // FK e Propriedade de Navegação para o Setor escolhido/vinculado
        public int? SetorId { get; set; }
        public Setor? Setor { get; set; }

        // FK e Propriedade de Navegação para o Número WhatsApp que recebeu o atendimento
        public int? NumeroWhatsAppId { get; set; }
        public NumeroWhatsApp? NumeroWhatsApp { get; set; }

        public StatusAtendimento Status { get; set; } = StatusAtendimento.EmUra;
        public DateTime DataInicio { get; set; } = DateTime.UtcNow;
        public DateTime? DataFim { get; set; }

        // Propriedade de navegação (Um atendimento possui um histórico de mensagens)
        public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
    }
}
