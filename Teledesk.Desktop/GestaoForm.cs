using System.Net.Http.Headers;
using System.Net.Http.Json;
using Teledesk.Desktop.Models;

namespace Teledesk.Desktop;

public class GestaoForm : Form
{
    private readonly AtendenteModel _atendenteLogado;
    private readonly string _baseUrl = "http://localhost:5000";

    private TabControl tabControl = null!;

    // Aba Setores
    private ListBox lstSetores = null!;
    private TextBox txtSetorNome = null!;
    private TextBox txtSetorDescricao = null!;
    private NumericUpDown numOrdemUra = null!;
    private CheckBox chkSetorAtivo = null!;
    private int? _setorEditandoId;

    // Aba Números WhatsApp
    private ListBox lstNumeros = null!;
    private TextBox txtNumeroNome = null!;
    private TextBox txtNumeroExibicao = null!;
    private TextBox txtPhoneNumberId = null!;
    private TextBox txtAccessToken = null!;
    private CheckBox chkNumeroAtivo = null!;
    private CheckedListBox clbNumeroSetores = null!;
    private int? _numeroEditandoId;

    // Aba Atendentes
    private ListBox lstAtendentes = null!;
    private TextBox txtAtendenteNome = null!;
    private TextBox txtAtendenteEmail = null!;
    private TextBox txtAtendenteSenha = null!;
    private ComboBox cmbAtendentePerfil = null!;
    private CheckBox chkAtendenteAtivo = null!;
    private CheckedListBox clbAtendenteSetores = null!;
    private int? _atendenteEditandoId;

    // Aba Simulador
    private ComboBox cmbSimuladorNumero = null!;
    private TextBox txtSimuladorWaId = null!;
    private TextBox txtSimuladorNome = null!;
    private TextBox txtSimuladorTexto = null!;
    private TextBox txtSimuladorResultado = null!;

    public GestaoForm(AtendenteModel atendenteLogado)
    {
        _atendenteLogado = atendenteLogado;
        Text = "Teledesk - Gestão Multi-Setor, Números WhatsApp e Simulador";
        Size = new Size(920, 620);
        StartPosition = FormStartPosition.CenterParent;

        InicializarInterface();
        Load += async (s, e) => await CarregarTodosDadosAsync();
    }

    private void InicializarInterface()
    {
        tabControl = new TabControl { Dock = DockStyle.Fill };

        var tabSetores = new TabPage("Setores");
        var tabNumeros = new TabPage("Números WhatsApp");
        var tabAtendentes = new TabPage("Atendentes");
        var tabSimulador = new TabPage("Simulador WhatsApp");

        MontarAbaSetores(tabSetores);
        MontarAbaNumeros(tabNumeros);
        MontarAbaAtendentes(tabAtendentes);
        MontarAbaSimulador(tabSimulador);

        if (_atendenteLogado.Perfil == 1)
        {
            tabControl.TabPages.Add(tabSetores);
            tabControl.TabPages.Add(tabNumeros);
            tabControl.TabPages.Add(tabAtendentes);
        }
        tabControl.TabPages.Add(tabSimulador);

        Controls.Add(tabControl);
    }

    private void MontarAbaSetores(TabPage page)
    {
        lstSetores = new ListBox { Location = new Point(16, 16), Size = new Size(420, 480) };
        lstSetores.SelectedIndexChanged += (s, e) =>
        {
            if (lstSetores.SelectedItem is SetorViewModel item)
            {
                _setorEditandoId = item.Id;
                txtSetorNome.Text = item.Nome;
                txtSetorDescricao.Text = item.Descricao ?? string.Empty;
                numOrdemUra.Value = Math.Max(0, item.OrdemUra);
                chkSetorAtivo.Checked = item.Ativo;
            }
        };

        var lblNome = new Label { Text = "Nome do Setor:", Location = new Point(460, 20), AutoSize = true };
        txtSetorNome = new TextBox { Location = new Point(460, 42), Size = new Size(390, 23) };

        var lblDesc = new Label { Text = "Descrição (exibida na URA):", Location = new Point(460, 78), AutoSize = true };
        txtSetorDescricao = new TextBox { Location = new Point(460, 100), Size = new Size(390, 23) };

        var lblOrdem = new Label { Text = "Ordem na URA:", Location = new Point(460, 136), AutoSize = true };
        numOrdemUra = new NumericUpDown { Location = new Point(460, 158), Size = new Size(120, 23), Minimum = 1, Maximum = 99, Value = 1 };

        chkSetorAtivo = new CheckBox { Text = "Setor Ativo", Location = new Point(460, 198), Checked = true, AutoSize = true };

        var btnNovo = new Button { Text = "Novo", Location = new Point(460, 240), Size = new Size(100, 30) };
        btnNovo.Click += (s, e) => LimparFormularioSetor();

        var btnSalvar = new Button { Text = "Salvar Setor", Location = new Point(575, 240), Size = new Size(130, 30) };
        btnSalvar.Click += async (s, e) => await SalvarSetorAsync();

        page.Controls.AddRange(new Control[]
        {
            lstSetores, lblNome, txtSetorNome, lblDesc, txtSetorDescricao,
            lblOrdem, numOrdemUra, chkSetorAtivo, btnNovo, btnSalvar
        });
    }

    private void MontarAbaNumeros(TabPage page)
    {
        lstNumeros = new ListBox { Location = new Point(16, 16), Size = new Size(420, 480) };
        lstNumeros.SelectedIndexChanged += (s, e) =>
        {
            if (lstNumeros.SelectedItem is NumeroWhatsAppViewModel item)
            {
                _numeroEditandoId = item.Id;
                txtNumeroNome.Text = item.NomeIdentificacao;
                txtNumeroExibicao.Text = item.NumeroExibicao;
                txtPhoneNumberId.Text = item.PhoneNumberId;
                txtAccessToken.Text = string.Empty;
                chkNumeroAtivo.Checked = item.Ativo;

                for (int i = 0; i < clbNumeroSetores.Items.Count; i++)
                {
                    if (clbNumeroSetores.Items[i] is SetorResumoModel setor)
                    {
                        clbNumeroSetores.SetItemChecked(i, item.Setores.Any(x => x.Id == setor.Id));
                    }
                }
            }
        };

        var lblNome = new Label { Text = "Nome de Identificação:", Location = new Point(460, 20), AutoSize = true };
        txtNumeroNome = new TextBox { Location = new Point(460, 42), Size = new Size(390, 23) };

        var lblNum = new Label { Text = "Número de Exibição (ex: +55 11 99999-9999):", Location = new Point(460, 76), AutoSize = true };
        txtNumeroExibicao = new TextBox { Location = new Point(460, 98), Size = new Size(390, 23) };

        var lblPhoneId = new Label { Text = "PhoneNumberId (Meta Cloud API):", Location = new Point(460, 132), AutoSize = true };
        txtPhoneNumberId = new TextBox { Location = new Point(460, 154), Size = new Size(390, 23) };

        var lblToken = new Label { Text = "Access Token próprio (opcional):", Location = new Point(460, 188), AutoSize = true };
        txtAccessToken = new TextBox { Location = new Point(460, 210), Size = new Size(390, 23), UseSystemPasswordChar = true };

        var lblSetores = new Label { Text = "Setores atendidos (1 = Direto | 2+ = URA):", Location = new Point(460, 244), AutoSize = true };
        clbNumeroSetores = new CheckedListBox { Location = new Point(460, 266), Size = new Size(390, 150), CheckOnClick = true };

        chkNumeroAtivo = new CheckBox { Text = "Número Ativo", Location = new Point(460, 428), Checked = true, AutoSize = true };

        var btnNovo = new Button { Text = "Novo", Location = new Point(460, 464), Size = new Size(100, 30) };
        btnNovo.Click += (s, e) => LimparFormularioNumero();

        var btnSalvar = new Button { Text = "Salvar Número", Location = new Point(575, 464), Size = new Size(140, 30) };
        btnSalvar.Click += async (s, e) => await SalvarNumeroAsync();

        page.Controls.AddRange(new Control[]
        {
            lstNumeros, lblNome, txtNumeroNome, lblNum, txtNumeroExibicao,
            lblPhoneId, txtPhoneNumberId, lblToken, txtAccessToken,
            lblSetores, clbNumeroSetores, chkNumeroAtivo, btnNovo, btnSalvar
        });
    }

    private void MontarAbaAtendentes(TabPage page)
    {
        lstAtendentes = new ListBox { Location = new Point(16, 16), Size = new Size(420, 480) };
        lstAtendentes.SelectedIndexChanged += (s, e) =>
        {
            if (lstAtendentes.SelectedItem is AtendenteViewModel item)
            {
                _atendenteEditandoId = item.Id;
                txtAtendenteNome.Text = item.Nome;
                txtAtendenteEmail.Text = item.Email;
                txtAtendenteSenha.Text = string.Empty;
                cmbAtendentePerfil.SelectedIndex = item.Perfil == 1 ? 1 : 0;
                chkAtendenteAtivo.Checked = item.Ativo;

                for (int i = 0; i < clbAtendenteSetores.Items.Count; i++)
                {
                    if (clbAtendenteSetores.Items[i] is SetorResumoModel setor)
                    {
                        clbAtendenteSetores.SetItemChecked(i, item.Setores.Any(x => x.Id == setor.Id));
                    }
                }
            }
        };

        var lblNome = new Label { Text = "Nome:", Location = new Point(460, 20), AutoSize = true };
        txtAtendenteNome = new TextBox { Location = new Point(460, 42), Size = new Size(390, 23) };

        var lblEmail = new Label { Text = "E-mail:", Location = new Point(460, 76), AutoSize = true };
        txtAtendenteEmail = new TextBox { Location = new Point(460, 98), Size = new Size(390, 23) };

        var lblSenha = new Label { Text = "Senha (deixe vazio ao editar para manter):", Location = new Point(460, 132), AutoSize = true };
        txtAtendenteSenha = new TextBox { Location = new Point(460, 154), Size = new Size(390, 23), UseSystemPasswordChar = true };

        var lblPerfil = new Label { Text = "Perfil:", Location = new Point(460, 188), AutoSize = true };
        cmbAtendentePerfil = new ComboBox { Location = new Point(460, 210), Size = new Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbAtendentePerfil.Items.AddRange(new object[] { "Atendente", "Admin" });
        cmbAtendentePerfil.SelectedIndex = 0;

        chkAtendenteAtivo = new CheckBox { Text = "Usuário Ativo", Location = new Point(680, 212), Checked = true, AutoSize = true };

        var lblSetores = new Label { Text = "Setores do Atendente:", Location = new Point(460, 246), AutoSize = true };
        clbAtendenteSetores = new CheckedListBox { Location = new Point(460, 268), Size = new Size(390, 170), CheckOnClick = true };

        var btnNovo = new Button { Text = "Novo", Location = new Point(460, 460), Size = new Size(100, 30) };
        btnNovo.Click += (s, e) => LimparFormularioAtendente();

        var btnSalvar = new Button { Text = "Salvar Atendente", Location = new Point(575, 460), Size = new Size(150, 30) };
        btnSalvar.Click += async (s, e) => await SalvarAtendenteAsync();

        page.Controls.AddRange(new Control[]
        {
            lstAtendentes, lblNome, txtAtendenteNome, lblEmail, txtAtendenteEmail,
            lblSenha, txtAtendenteSenha, lblPerfil, cmbAtendentePerfil, chkAtendenteAtivo,
            lblSetores, clbAtendenteSetores, btnNovo, btnSalvar
        });
    }

    private void MontarAbaSimulador(TabPage page)
    {
        var lblNumero = new Label { Text = "Número WhatsApp de Destino:", Location = new Point(24, 20), AutoSize = true };
        cmbSimuladorNumero = new ComboBox { Location = new Point(24, 42), Size = new Size(440, 23), DropDownStyle = ComboBoxStyle.DropDownList };

        var lblWaId = new Label { Text = "Telefone/WhatsAppId do Cliente:", Location = new Point(24, 80), AutoSize = true };
        txtSimuladorWaId = new TextBox { Location = new Point(24, 102), Size = new Size(210, 23), Text = "5511999998888" };

        var lblNome = new Label { Text = "Nome do Cliente:", Location = new Point(254, 80), AutoSize = true };
        txtSimuladorNome = new TextBox { Location = new Point(254, 102), Size = new Size(210, 23), Text = "Cliente Teste" };

        var lblTexto = new Label { Text = "Mensagem enviada pelo cliente:", Location = new Point(24, 140), AutoSize = true };
        txtSimuladorTexto = new TextBox { Location = new Point(24, 162), Size = new Size(440, 23), Text = "Olá" };

        var btnEnviarSimulacao = new Button { Text = "Simular Recebimento WhatsApp", Location = new Point(24, 202), Size = new Size(240, 34) };
        btnEnviarSimulacao.Click += async (s, e) => await SimularMensagemAsync();

        var lblRes = new Label { Text = "Retorno da URA / Roteamento:", Location = new Point(24, 254), AutoSize = true };
        txtSimuladorResultado = new TextBox
        {
            Location = new Point(24, 276),
            Size = new Size(820, 220),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };

        page.Controls.AddRange(new Control[]
        {
            lblNumero, cmbSimuladorNumero, lblWaId, txtSimuladorWaId,
            lblNome, txtSimuladorNome, lblTexto, txtSimuladorTexto,
            btnEnviarSimulacao, lblRes, txtSimuladorResultado
        });
    }

    private HttpClient CriarHttpClientAutenticado()
    {
        var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _atendenteLogado.Token);
        return client;
    }

    private async Task CarregarTodosDadosAsync()
    {
        try
        {
            using var client = CriarHttpClientAutenticado();

            var setores = await client.GetFromJsonAsync<List<SetorViewModel>>("/api/setores") ?? new();
            var numeros = await client.GetFromJsonAsync<List<NumeroWhatsAppViewModel>>("/api/numeroswhatsapp") ?? new();

            lstSetores.Items.Clear();
            clbNumeroSetores.Items.Clear();
            clbAtendenteSetores.Items.Clear();
            foreach (var s in setores)
            {
                lstSetores.Items.Add(s);
                if (s.Ativo)
                {
                    var resumo = new SetorResumoModel { Id = s.Id, Nome = s.Nome };
                    clbNumeroSetores.Items.Add(resumo);
                    clbAtendenteSetores.Items.Add(resumo);
                }
            }

            lstNumeros.Items.Clear();
            cmbSimuladorNumero.Items.Clear();
            foreach (var n in numeros)
            {
                lstNumeros.Items.Add(n);
                if (n.Ativo)
                    cmbSimuladorNumero.Items.Add(n);
            }
            if (cmbSimuladorNumero.Items.Count > 0)
                cmbSimuladorNumero.SelectedIndex = 0;

            if (_atendenteLogado.Perfil == 1)
            {
                var atendentes = await client.GetFromJsonAsync<List<AtendenteViewModel>>("/api/atendentes") ?? new();
                lstAtendentes.Items.Clear();
                foreach (var a in atendentes)
                {
                    lstAtendentes.Items.Add(a);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar dados: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SalvarSetorAsync()
    {
        try
        {
            using var client = CriarHttpClientAutenticado();
            var payload = new
            {
                nome = txtSetorNome.Text.Trim(),
                descricao = txtSetorDescricao.Text.Trim(),
                ordemUra = (int)numOrdemUra.Value,
                ativo = chkSetorAtivo.Checked
            };

            var response = _setorEditandoId.HasValue
                ? await client.PutAsJsonAsync($"/api/setores/{_setorEditandoId.Value}", payload)
                : await client.PostAsJsonAsync("/api/setores", payload);

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), "Erro ao salvar setor");
                return;
            }

            LimparFormularioSetor();
            await CarregarTodosDadosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar setor: {ex.Message}");
        }
    }

    private async Task SalvarNumeroAsync()
    {
        try
        {
            var setoresIds = clbNumeroSetores.CheckedItems
                .OfType<SetorResumoModel>()
                .Select(s => s.Id)
                .ToList();

            using var client = CriarHttpClientAutenticado();
            var payload = new
            {
                nomeIdentificacao = txtNumeroNome.Text.Trim(),
                numeroExibicao = txtNumeroExibicao.Text.Trim(),
                phoneNumberId = txtPhoneNumberId.Text.Trim(),
                accessToken = string.IsNullOrWhiteSpace(txtAccessToken.Text) ? null : txtAccessToken.Text.Trim(),
                ativo = chkNumeroAtivo.Checked,
                setoresIds
            };

            var response = _numeroEditandoId.HasValue
                ? await client.PutAsJsonAsync($"/api/numeroswhatsapp/{_numeroEditandoId.Value}", payload)
                : await client.PostAsJsonAsync("/api/numeroswhatsapp", payload);

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), "Erro ao salvar número");
                return;
            }

            LimparFormularioNumero();
            await CarregarTodosDadosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar número: {ex.Message}");
        }
    }

    private async Task SalvarAtendenteAsync()
    {
        try
        {
            var setoresIds = clbAtendenteSetores.CheckedItems
                .OfType<SetorResumoModel>()
                .Select(s => s.Id)
                .ToList();

            using var client = CriarHttpClientAutenticado();
            var payload = new
            {
                nome = txtAtendenteNome.Text.Trim(),
                email = txtAtendenteEmail.Text.Trim(),
                senha = string.IsNullOrWhiteSpace(txtAtendenteSenha.Text) ? null : txtAtendenteSenha.Text,
                perfil = cmbAtendentePerfil.SelectedIndex == 1 ? 1 : 0,
                ativo = chkAtendenteAtivo.Checked,
                setoresIds
            };

            var response = _atendenteEditandoId.HasValue
                ? await client.PutAsJsonAsync($"/api/atendentes/{_atendenteEditandoId.Value}", payload)
                : await client.PostAsJsonAsync("/api/atendentes", payload);

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(await response.Content.ReadAsStringAsync(), "Erro ao salvar atendente");
                return;
            }

            LimparFormularioAtendente();
            await CarregarTodosDadosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar atendente: {ex.Message}");
        }
    }

    private async Task SimularMensagemAsync()
    {
        try
        {
            var numero = cmbSimuladorNumero.SelectedItem as NumeroWhatsAppViewModel;
            var phoneNumberId = numero?.PhoneNumberId ?? "100000000000000";

            using var client = CriarHttpClientAutenticado();
            var response = await client.PostAsJsonAsync("/api/webhook/whatsapp/simular", new
            {
                phoneNumberId,
                waId = txtSimuladorWaId.Text.Trim(),
                nomeCliente = txtSimuladorNome.Text.Trim(),
                texto = txtSimuladorTexto.Text.Trim()
            });

            if (!response.IsSuccessStatusCode)
            {
                txtSimuladorResultado.Text = await response.Content.ReadAsStringAsync();
                return;
            }

            var resultado = await response.Content.ReadFromJsonAsync<ResultadoSimulacaoModel>();
            if (resultado != null)
            {
                txtSimuladorResultado.Text =
                    $"Status: {resultado.Observacao}{Environment.NewLine}" +
                    $"Atendimento ID: {resultado.AtendimentoId} | Setor: {resultado.SetorNome ?? "Em URA"}{Environment.NewLine}" +
                    $"--- Resposta enviada ao cliente no WhatsApp ---{Environment.NewLine}" +
                    (resultado.RespostaAutomatica ?? "(Sem resposta automática - mensagem encaminhada para o atendente)");
            }
        }
        catch (Exception ex)
        {
            txtSimuladorResultado.Text = $"Erro ao simular: {ex.Message}";
        }
    }

    private void LimparFormularioSetor()
    {
        _setorEditandoId = null;
        txtSetorNome.Clear();
        txtSetorDescricao.Clear();
        numOrdemUra.Value = 1;
        chkSetorAtivo.Checked = true;
    }

    private void LimparFormularioNumero()
    {
        _numeroEditandoId = null;
        txtNumeroNome.Clear();
        txtNumeroExibicao.Clear();
        txtPhoneNumberId.Clear();
        txtAccessToken.Clear();
        chkNumeroAtivo.Checked = true;
        for (int i = 0; i < clbNumeroSetores.Items.Count; i++)
            clbNumeroSetores.SetItemChecked(i, false);
    }

    private void LimparFormularioAtendente()
    {
        _atendenteEditandoId = null;
        txtAtendenteNome.Clear();
        txtAtendenteEmail.Clear();
        txtAtendenteSenha.Clear();
        cmbAtendentePerfil.SelectedIndex = 0;
        chkAtendenteAtivo.Checked = true;
        for (int i = 0; i < clbAtendenteSetores.Items.Count; i++)
            clbAtendenteSetores.SetItemChecked(i, false);
    }
}
