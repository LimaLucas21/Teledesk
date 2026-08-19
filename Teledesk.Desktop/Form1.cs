using Teledesk.Desktop.Models;

namespace Teledesk.Desktop;

public partial class Form1 : Form
{
    private readonly AtendenteModel? _atendenteLogado;

    public Form1()
    {
        InitializeComponent();
    }

    public Form1(AtendenteModel atendenteLogado) : this()
    {
        _atendenteLogado = atendenteLogado;
        this.Text = $"Teledesk - Painel do Atendente ({_atendenteLogado.Nome})";
    }
}
