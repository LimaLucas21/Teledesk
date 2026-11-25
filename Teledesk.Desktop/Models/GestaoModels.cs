namespace Teledesk.Desktop.Models
{
    public class SetorResumoModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public override string ToString() => Nome;
    }

    public class SetorViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int OrdemUra { get; set; }
        public bool Ativo { get; set; }
        public int QuantidadeAtendentes { get; set; }
        public int QuantidadeNumeros { get; set; }

        public override string ToString()
        {
            var status = Ativo ? "Ativo" : "Inativo";
            return $"[{OrdemUra}] {Nome} ({status}) - {QuantidadeAtendentes} atend. / {QuantidadeNumeros} núm.";
        }
    }

    public class NumeroWhatsAppViewModel
    {
        public int Id { get; set; }
        public string NomeIdentificacao { get; set; } = string.Empty;
        public string NumeroExibicao { get; set; } = string.Empty;
        public string PhoneNumberId { get; set; } = string.Empty;
        public bool PossuiTokenProprio { get; set; }
        public bool Ativo { get; set; }
        public List<SetorResumoModel> Setores { get; set; } = new();

        public override string ToString()
        {
            var setoresTxt = Setores.Count == 0
                ? "Sem setor"
                : Setores.Count == 1
                    ? $"Direto: {Setores[0].Nome}"
                    : $"URA ({Setores.Count} setores)";
            return $"{NomeIdentificacao} | {NumeroExibicao} [{setoresTxt}]";
        }
    }

    public class AtendenteViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Perfil { get; set; }
        public int Status { get; set; }
        public bool Ativo { get; set; }
        public List<SetorResumoModel> Setores { get; set; } = new();

        public override string ToString()
        {
            var perfilTxt = Perfil == 1 ? "Admin" : "Atendente";
            var setoresTxt = Setores.Count > 0 ? string.Join(", ", Setores.Select(s => s.Nome)) : "Sem setor";
            return $"{Nome} ({perfilTxt}) - [{setoresTxt}]";
        }
    }

    public class ResultadoSimulacaoModel
    {
        public bool Sucesso { get; set; }
        public int? AtendimentoId { get; set; }
        public int? SetorId { get; set; }
        public string? SetorNome { get; set; }
        public string? RespostaAutomatica { get; set; }
        public string Observacao { get; set; } = string.Empty;
    }
}
