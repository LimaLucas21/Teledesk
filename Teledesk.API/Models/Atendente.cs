using Teledesk.API.Models.Enums;

namespace Teledesk.API.Models
{
    public class Atendente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public PerfilAtendente Perfil { get; set; } = PerfilAtendente.Atendente;
        public StatusAtendente Status { get; set; } = StatusAtendente.Offline;
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Propriedades de navegação
        public ICollection<Atendimento> Atendimentos { get; set; } = new List<Atendimento>();
        public ICollection<Setor> Setores { get; set; } = new List<Setor>();
    }
}
