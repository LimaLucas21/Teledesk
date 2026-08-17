namespace Teledesk.API.Models
{
    public class Contato
    {
        public int Id { get; set; }
        public long TelegramChatId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Username { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Propriedade de navegação (Um contato pode ter múltiplos atendimentos)
        public ICollection<Atendimento> Atendimentos { get; set; } = new List<Atendimento>();
    }
}
