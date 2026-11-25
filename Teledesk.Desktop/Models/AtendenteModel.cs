namespace Teledesk.Desktop.Models
{
    public class AtendenteModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Perfil { get; set; } // 0 = Atendente, 1 = Admin
        public int Status { get; set; }
        public string Token { get; set; } = string.Empty;
        public List<SetorResumoModel> Setores { get; set; } = new();
    }
}
