using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Teledesk.Desktop.Models;


namespace Teledesk.Desktop
{
    public partial class LoginForm : Form
    {
        public AtendenteModel? AtendenteLogado { get; private set; }

        public LoginForm()
        {
            InitializeComponent();
            txtSenha.UseSystemPasswordChar = true;
            this.AcceptButton = btnEntrar;
        }

        private async void btnEntrar_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                MessageBox.Show("Credenciais Incorretas!");
                return;
            }
            try
            {
                btnEntrar.Enabled = false;
                btnEntrar.Text = "Entrando...";
                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync("http://localhost:5000/api/atendentes/login", new
                {
                    email = email,
                    senha = senha
                });
                if (response.IsSuccessStatusCode)
                {
                    var atendente = await response.Content.ReadFromJsonAsync<AtendenteModel>();
                    this.AtendenteLogado = atendente;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    var mensagemErro = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(mensagemErro, "Falha no Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar com a API: {ex.Message}", "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnEntrar.Enabled = true;
                btnEntrar.Text = "Entrar";
            }
        }
    }
}
