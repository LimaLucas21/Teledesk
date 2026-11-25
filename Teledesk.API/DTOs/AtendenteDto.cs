using Teledesk.API.Models.Enums;

namespace Teledesk.API.DTOs
{
    public class AtendenteDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public PerfilAtendente Perfil { get; set; }
        public StatusAtendente Status { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<SetorResumoDto> Setores { get; set; } = new();
    }

    public class SalvarAtendenteDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Senha { get; set; }
        public PerfilAtendente Perfil { get; set; } = PerfilAtendente.Atendente;
        public bool Ativo { get; set; } = true;
        public List<int> SetoresIds { get; set; } = new();
    }
}

