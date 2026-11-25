namespace Teledesk.API.DTOs
{
    public class SetorDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int OrdemUra { get; set; }
        public bool Ativo { get; set; }
        public int QuantidadeAtendentes { get; set; }
        public int QuantidadeNumeros { get; set; }
    }

    public class SalvarSetorDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int OrdemUra { get; set; } = 1;
        public bool Ativo { get; set; } = true;
    }
}

