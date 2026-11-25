using Teledesk.API.Models.Enums;

namespace Teledesk.API.DTOs
{
    public class LoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class SetorResumoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public PerfilAtendente Perfil { get; set; }
        public StatusAtendente Status { get; set; }
        public string Token { get; set; } = string.Empty;
        public List<SetorResumoDto> Setores { get; set; } = new();
    }
}
