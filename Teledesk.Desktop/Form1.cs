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
        this.Text = $"Teledesk - Painel do Atendente ({_atendenteLogado?.Nome})";

        // Vincula os eventos dos botões e da lista
        this.Load += async (s, e) => await InicializarSignalRAsync();
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

    /// <summary>
    /// Configura e inicializa a conexão com o Hub do SignalR
    /// </summary>
    private async Task InicializarSignalRAsync()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/hubs/atendimento")
                .WithAutomaticReconnect()
                .Build();

            // 1. Nova conversa entrou na fila (Status: AguardandoAtendente)
            _hubConnection.On<AtendimentoModel>("NovaConversaNaFila", (atendimento) =>
            {
                this.Invoke(() => AdicionarOuAtualizarAtendimento(atendimento));
            });

            // 2. Nova mensagem recebida (de cliente ou atendente)
            _hubConnection.On<MensagemModel>("NovaMensagemRecebida", (mensagem) =>
            {
                this.Invoke(() => ProcessarNovaMensagem(mensagem));
            });

            // 3. Um atendimento foi assumido por alguém
            _hubConnection.On<AtendimentoModel>("AtendimentoAssumido", (atendimento) =>
            {
                this.Invoke(() => AdicionarOuAtualizarAtendimento(atendimento));
            });

            // 4. Um atendimento foi finalizado
            _hubConnection.On<int>("AtendimentoFinalizado", (atendimentoId) =>
            {
                this.Invoke(() => RemoverAtendimento(atendimentoId));
            });

            await _hubConnection.StartAsync();

            // Carrega os atendimentos já existentes
            await CarregarFilaInicialAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao conectar ao SignalR: {ex.Message}", "Erro SignalR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Busca a lista inicial de atendimentos ativos do servidor
    /// </summary>
    private async Task CarregarFilaInicialAsync()
    {
        if (_hubConnection == null) return;

        try
        {
            var atendimentos = await _hubConnection.InvokeAsync<List<AtendimentoModel>>("ObterFilaAtendimento");
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

    /// <summary>
    /// Manipula a seleção de um item na lista de fila
    /// </summary>
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

        lblClienteInfo.Text = $"Atendimento #{_atendimentoSelecionado.Id} - {_atendimentoSelecionado.NomeCliente} | Chat ID: {_atendimentoSelecionado.TelegramChatId}";
        AtualizarEstadoBotoes();

        await CarregarHistoricoMensagensAsync(_atendimentoSelecionado.Id);
    }

    /// <summary>
    /// Carrega as mensagens trocadas no atendimento selecionado
    /// </summary>
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

            // Scroll automático para a última mensagem
            if (lstMensagens.Items.Count > 0)
                lstMensagens.SelectedIndex = lstMensagens.Items.Count - 1;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar histórico: {ex.Message}");
        }
    }

    /// <summary>
    /// Atendente clica para assumir o chamado selecionado
    /// </summary>
    private async Task AssumirAtendimentoAsync()
    {
        if (_hubConnection == null || _atendimentoSelecionado == null || _atendenteLogado == null) return;

        try
        {
            await _hubConnection.InvokeAsync("AssumirAtendimento", _atendimentoSelecionado.Id, _atendenteLogado.Id);
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

    /// <summary>
    /// Atendente clica para finalizar o chamado
    /// </summary>
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

    /// <summary>
    /// Envia a mensagem digitada para o Telegram do cliente via SignalR
    /// </summary>
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

    /// <summary>
    /// Processa uma nova mensagem recebida do SignalR
    /// </summary>
    private void ProcessarNovaMensagem(MensagemModel mensagem)
    {
        // Se a mensagem pertencer ao atendimento atualmente aberto na tela, exibe na lista de chat
        if (_atendimentoSelecionado != null && _atendimentoSelecionado.Id == mensagem.AtendimentoId)
        {
            lstMensagens.Items.Add(mensagem.FormatarParaExibicao());
            lstMensagens.SelectedIndex = lstMensagens.Items.Count - 1; // Auto-scroll
        }
    }

    /// <summary>
    /// Adiciona ou atualiza um item na ListBox de fila
    /// </summary>
    private void AdicionarOuAtualizarAtendimento(AtendimentoModel atendimento)
    {
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

    /// <summary>
    /// Remove um atendimento finalizado da ListBox
    /// </summary>
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

    /// <summary>
    /// Habilita ou desabilita botões e campos de acordo com a seleção atual
    /// </summary>
    private void AtualizarEstadoBotoes()
    {
        bool temSelecionado = _atendimentoSelecionado != null;
        bool emAtendimentoComigo = temSelecionado && 
                                  _atendimentoSelecionado!.Status == 2 && 
                                  _atendimentoSelecionado.AtendenteId == _atendenteLogado?.Id;

        btnAssumir.Enabled = temSelecionado && _atendimentoSelecionado!.Status == 1; // Status 1 = Aguardando
        btnFinalizar.Enabled = emAtendimentoComigo;
        txtMensagem.Enabled = emAtendimentoComigo;
        btnEnviar.Enabled = emAtendimentoComigo;
    }
}
