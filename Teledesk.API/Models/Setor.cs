namespace Teledesk.API.Models
{
    public class Setor
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int OrdemUra { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Relacionamentos N:N e 1:N
        public ICollection<Atendente> Atendentes { get; set; } = new List<Atendente>();
        public ICollection<NumeroWhatsApp> NumerosWhatsApp { get; set; } = new List<NumeroWhatsApp>();
        public ICollection<Atendimento> Atendimentos { get; set; } = new List<Atendimento>();
    }
}

