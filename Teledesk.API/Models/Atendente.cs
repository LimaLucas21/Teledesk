using Teledesk.API.Models.Enums;

namespace Teledesk.API.Models
{
    public class Atendente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;
        public StatusAtendente Status { get; set; } = StatusAtendente.Offline;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Propriedade de navegação (Um atendente pode realizar múltiplos atendimentos)
        public ICollection<Atendimento> Atendimentos { get; set; } = new List<Atendimento>();
    }
}
