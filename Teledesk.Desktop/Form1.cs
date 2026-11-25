using Microsoft.AspNetCore.SignalR.Client;
using Teledesk.Desktop.Models;

namespace Teledesk.Desktop;

public partial class Form1 : Form
{
    private readonly AtendenteModel? _atendenteLogado;
    private HubConnection? _hubConnection;
    private AtendimentoModel? _atendimentoSelecionado;

    public Form1()
    {
        InitializeComponent();
    }

    public Form1(AtendenteModel atendenteLogado) : this()
    {
        _atendenteLogado = atendenteLogado;
        var perfilTexto = _atendenteLogado.Perfil == 1 ? "Admin" : "Atendente";
        this.Text = $"Teledesk WhatsApp - {_atendenteLogado.Nome} ({perfilTexto})";
        this.label1.Text = $"Teledesk WhatsApp - {_atendenteLogado.Nome} ({perfilTexto})";

        CarregarComboSetores();

        this.Load += async (s, e) => await InicializarSignalRAsync();
        this.cmbFiltroSetor.SelectedIndexChanged += async (s, e) => await CarregarFilaInicialAsync();
        this.btnGestao.Click += async (s, e) => await AbrirGestaoAsync();
        this.lstAtendimentos.SelectedIndexChanged += async (s, e) => await SelecionarAtendimentoAsync();
        this.btnAssumir.Click += async (s, e) => await AssumirAtendimentoAsync();
        this.btnFinalizar.Click += async (s, e) => await FinalizarAtendimentoAsync();
        this.btnEnviar.Click += async (s, e) => await EnviarMensagemAsync();
        this.txtMensagem.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await EnviarMensagemAsync();
            }
        };

        AtualizarEstadoBotoes();
    }

    private void CarregarComboSetores()
    {
        cmbFiltroSetor.Items.Clear();
        cmbFiltroSetor.Items.Add(new SetorResumoModel { Id = 0, Nome = "Todos os meus setores" });
        if (_atendenteLogado?.Setores != null)
        {
            foreach (var setor in _atendenteLogado.Setores)
            {
                cmbFiltroSetor.Items.Add(setor);
            }
        }
        cmbFiltroSetor.SelectedIndex = 0;
    }

    private async Task AbrirGestaoAsync()
    {
        if (_atendenteLogado == null) return;
        using var gestao = new GestaoForm(_atendenteLogado);
        gestao.ShowDialog(this);
        await CarregarFilaInicialAsync();
    }

    /// <summary>
    /// Configura e inicializa a conexão autenticada via JWT com o Hub do SignalR
    /// </summary>
    private async Task InicializarSignalRAsync()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/hubs/atendimento", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_atendenteLogado?.Token);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<AtendimentoModel>("NovaConversaNaFila", (atendimento) =>
            {
                this.Invoke(() => AdicionarOuAtualizarAtendimento(atendimento));
            });

            _hubConnection.On<MensagemModel>("NovaMensagemRecebida", (mensagem) =>
            {
                this.Invoke(() => ProcessarNovaMensagem(mensagem));
            });

            _hubConnection.On<AtendimentoModel>("AtendimentoAssumido", (atendimento) =>
            {
                this.Invoke(() => AdicionarOuAtualizarAtendimento(atendimento));
            });

            _hubConnection.On<int>("AtendimentoFinalizado", (atendimentoId) =>
            {
                this.Invoke(() => RemoverAtendimento(atendimentoId));
            });

            await _hubConnection.StartAsync();
            await CarregarFilaInicialAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao conectar ao SignalR: {ex.Message}", "Erro SignalR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Busca a lista de atendimentos ativos do servidor filtrada por setor
    /// </summary>
    private async Task CarregarFilaInicialAsync()
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected) return;

        try
        {
            int? filtroSetorId = null;
            if (cmbFiltroSetor.SelectedItem is SetorResumoModel setor && setor.Id > 0)
            {
                filtroSetorId = setor.Id;
            }

            var atendimentos = await _hubConnection.InvokeAsync<List<AtendimentoModel>>("ObterFilaAtendimento", filtroSetorId);
            lstAtendimentos.Items.Clear();
            foreach (var a in atendimentos)
            {
                lstAtendimentos.Items.Add(a);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar fila inicial: {ex.Message}");
        }
    }

    private async Task SelecionarAtendimentoAsync()
    {
        _atendimentoSelecionado = lstAtendimentos.SelectedItem as AtendimentoModel;

        if (_atendimentoSelecionado == null)
        {
            lblClienteInfo.Text = "Selecione um cliente...";
            lstMensagens.Items.Clear();
            AtualizarEstadoBotoes();
            return;
        }

        var setor = _atendimentoSelecionado.SetorNome ?? "Sem Setor";
        var numero = _atendimentoSelecionado.NumeroExibicao ?? "-";
        lblClienteInfo.Text = $"#{_atendimentoSelecionado.Id} - {_atendimentoSelecionado.NomeCliente} | WhatsApp: {_atendimentoSelecionado.WhatsAppId} | Setor: {setor} | Nº: {numero}";
        AtualizarEstadoBotoes();

        await CarregarHistoricoMensagensAsync(_atendimentoSelecionado.Id);
    }

    private async Task CarregarHistoricoMensagensAsync(int atendimentoId)
    {
        if (_hubConnection == null) return;

        try
        {
            var historico = await _hubConnection.InvokeAsync<List<MensagemModel>>("ObterHistoricoMensagens", atendimentoId);
            lstMensagens.Items.Clear();
            foreach (var msg in historico)
            {
                lstMensagens.Items.Add(msg.FormatarParaExibicao());
            }

            if (lstMensagens.Items.Count > 0)
                lstMensagens.SelectedIndex = lstMensagens.Items.Count - 1;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar histórico: {ex.Message}");
        }
    }

    private async Task AssumirAtendimentoAsync()
    {
        if (_hubConnection == null || _atendimentoSelecionado == null || _atendenteLogado == null) return;

        try
        {
            var assumido = await _hubConnection.InvokeAsync<bool>("AssumirAtendimento", _atendimentoSelecionado.Id);
            if (!assumido)
            {
                MessageBox.Show("Este atendimento já foi assumido por outro operador ou não pertence ao seu setor.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await CarregarFilaInicialAsync();
                return;
            }

            _atendimentoSelecionado.Status = 2; // Em Atendimento
            _atendimentoSelecionado.AtendenteId = _atendenteLogado.Id;
            _atendimentoSelecionado.AtendenteNome = _atendenteLogado.Nome;

            AdicionarOuAtualizarAtendimento(_atendimentoSelecionado);
            AtualizarEstadoBotoes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao assumir atendimento: {ex.Message}");
        }
    }

    private async Task FinalizarAtendimentoAsync()
    {
        if (_hubConnection == null || _atendimentoSelecionado == null) return;

        var confirmacao = MessageBox.Show(
            $"Deseja realmente finalizar o atendimento de {_atendimentoSelecionado.NomeCliente}?",
            "Finalizar Atendimento",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmacao != DialogResult.Yes) return;

        try
        {
            await _hubConnection.InvokeAsync("FinalizarAtendimento", _atendimentoSelecionado.Id);
            RemoverAtendimento(_atendimentoSelecionado.Id);
            _atendimentoSelecionado = null;
            lblClienteInfo.Text = "Selecione um cliente...";
            lstMensagens.Items.Clear();
            AtualizarEstadoBotoes();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao finalizar atendimento: {ex.Message}");
        }
    }

    private async Task EnviarMensagemAsync()
    {
        if (_hubConnection == null || _atendimentoSelecionado == null) return;

        var texto = txtMensagem.Text.Trim();
        if (string.IsNullOrEmpty(texto)) return;

        try
        {
            txtMensagem.Clear();
            await _hubConnection.InvokeAsync("EnviarMensagemParaCliente", _atendimentoSelecionado.Id, texto);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao enviar mensagem: {ex.Message}");
        }
    }

    private void ProcessarNovaMensagem(MensagemModel mensagem)
    {
        if (_atendimentoSelecionado != null && _atendimentoSelecionado.Id == mensagem.AtendimentoId)
        {
            lstMensagens.Items.Add(mensagem.FormatarParaExibicao());
            lstMensagens.SelectedIndex = lstMensagens.Items.Count - 1;
        }
    }

    private void AdicionarOuAtualizarAtendimento(AtendimentoModel atendimento)
    {
        if (cmbFiltroSetor.SelectedItem is SetorResumoModel setor && setor.Id > 0 && atendimento.SetorId != setor.Id)
            return;

        for (int i = 0; i < lstAtendimentos.Items.Count; i++)
        {
            if (lstAtendimentos.Items[i] is AtendimentoModel item && item.Id == atendimento.Id)
            {
                lstAtendimentos.Items[i] = atendimento;
                return;
            }
        }

        lstAtendimentos.Items.Add(atendimento);
    }

    private void RemoverAtendimento(int atendimentoId)
    {
        for (int i = 0; i < lstAtendimentos.Items.Count; i++)
        {
            if (lstAtendimentos.Items[i] is AtendimentoModel item && item.Id == atendimentoId)
            {
                lstAtendimentos.Items.RemoveAt(i);
                break;
            }
        }
    }

    private void AtualizarEstadoBotoes()
    {
        bool temSelecionado = _atendimentoSelecionado != null;
        bool emAtendimentoComigo = temSelecionado &&
                                  _atendimentoSelecionado!.Status == 2 &&
                                  _atendimentoSelecionado.AtendenteId == _atendenteLogado?.Id;

        btnAssumir.Enabled = temSelecionado && (_atendimentoSelecionado!.Status == 1 || _atendimentoSelecionado.Status == 0);
        btnFinalizar.Enabled = emAtendimentoComigo;
        txtMensagem.Enabled = emAtendimentoComigo;
        btnEnviar.Enabled = emAtendimentoComigo;
    }
}
