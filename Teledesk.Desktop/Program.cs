namespace Teledesk.Desktop;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var loginForm = new LoginForm();
        if (loginForm.ShowDialog() == DialogResult.OK && loginForm.AtendenteLogado != null)
        {
            Application.Run(new Form1(loginForm.AtendenteLogado));
        }
    }    
}