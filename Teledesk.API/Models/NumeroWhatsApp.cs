namespace Teledesk.API.Models
{
    public class NumeroWhatsApp
    {
        public int Id { get; set; }
        public string NomeIdentificacao { get; set; } = string.Empty;
        public string NumeroExibicao { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relacionamento N:N com Setores (1 número -> 1 setor = direto; 1 número -> N setores = URA)
        public ICollection<Setor> Setores { get; set; } = new List<Setor>();
        public ICollection<Atendimento> Atendimentos { get; set; } = new List<Atendimento>();
    }
}

