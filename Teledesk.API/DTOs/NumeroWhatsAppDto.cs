namespace Teledesk.API.DTOs
{
    public class NumeroWhatsAppDto
    {
        public int Id { get; set; }
        public string NomeIdentificacao { get; set; } = string.Empty;
        public string NumeroExibicao { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public bool PossuiTokenProprio { get; set; }
        public bool Ativo { get; set; }
        public List<SetorResumoDto> Setores { get; set; } = new();
    }

    public class SalvarNumeroWhatsAppDto
    {
        public string NomeIdentificacao { get; set; } = string.Empty;
        public string NumeroExibicao { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public bool Ativo { get; set; } = true;
        public List<int> SetoresIds { get; set; } = new();
    }
}

