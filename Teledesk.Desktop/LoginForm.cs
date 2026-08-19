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
        }

        private async void btnEntrar_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Credenciais Incorretas!");
                return;
            }

            try
            {
                using var client = new HttpClient();
                var atendente = await client.GetFromJsonAsync<AtendenteModel>($"http://localhost:5000/api/atendentes/login?email={email}");
                if (atendente != null)
                {
                    this.AtendenteLogado = atendente;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Atendente não encontrado!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar com a API: {ex.Message}");
            }
        }
    }
}
