namespace Teledesk.Desktop;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        label1 = new Label();
        label2 = new Label();
        btnAssumir = new Button();
        btnFinalizar = new Button();
        lblClienteInfo = new Label();
        lstMensagens = new ListBox();
        txtMensagem = new TextBox();
        btnEnviar = new Button();
        lstAtendimentos = new ListBox();
        SuspendLayout();
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(352, 35);
        label1.Name = "label1";
        label1.Size = new Size(252, 15);
        label1.TabIndex = 0;
        label1.Text = " Teledesk - Painel do Atendente (Lucas Lima)   ";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(63, 134);
        label2.Name = "label2";
        label2.Size = new Size(117, 15);
        label2.TabIndex = 1;
        label2.Text = "Fila de Atendimento:";
        // 
        // btnAssumir
        // 
        btnAssumir.Location = new Point(84, 539);
        btnAssumir.Name = "btnAssumir";
        btnAssumir.Size = new Size(75, 23);
        btnAssumir.TabIndex = 2;
        btnAssumir.Text = "Assumir";
        btnAssumir.UseVisualStyleBackColor = true;
        // 
        // btnFinalizar
        // 
        btnFinalizar.Location = new Point(198, 539);
        btnFinalizar.Name = "btnFinalizar";
        btnFinalizar.Size = new Size(75, 23);
        btnFinalizar.TabIndex = 3;
        btnFinalizar.Text = "Finalizar";
        btnFinalizar.UseVisualStyleBackColor = true;
        // 
        // lblClienteInfo
        // 
        lblClienteInfo.AutoSize = true;
        lblClienteInfo.Location = new Point(373, 134);
        lblClienteInfo.Name = "lblClienteInfo";
        lblClienteInfo.Size = new Size(125, 15);
        lblClienteInfo.TabIndex = 4;
        lblClienteInfo.Text = "Selecione um cliente...";
        // 
        // lstMensagens
        // 
        lstMensagens.FormattingEnabled = true;
        lstMensagens.Location = new Point(373, 193);
        lstMensagens.Name = "lstMensagens";
        lstMensagens.Size = new Size(540, 319);
        lstMensagens.TabIndex = 5;
        // 
        // txtMensagem
        // 
        txtMensagem.Location = new Point(373, 540);
        txtMensagem.Name = "txtMensagem";
        txtMensagem.PlaceholderText = "Digite aqui...";
        txtMensagem.Size = new Size(427, 23);
        txtMensagem.TabIndex = 6;
        // 
        // btnEnviar
        // 
        btnEnviar.Location = new Point(838, 539);
        btnEnviar.Name = "btnEnviar";
        btnEnviar.Size = new Size(75, 23);
        btnEnviar.TabIndex = 7;
        btnEnviar.Text = "Enviar";
        btnEnviar.UseVisualStyleBackColor = true;
        // 
        // lstAtendimentos
        // 
        lstAtendimentos.FormattingEnabled = true;
        lstAtendimentos.Location = new Point(63, 193);
        lstAtendimentos.Name = "lstAtendimentos";
        lstAtendimentos.Size = new Size(229, 319);
        lstAtendimentos.TabIndex = 8;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 621);
        Controls.Add(lstAtendimentos);
        Controls.Add(btnEnviar);
        Controls.Add(txtMensagem);
        Controls.Add(lstMensagens);
        Controls.Add(lblClienteInfo);
        Controls.Add(btnFinalizar);
        Controls.Add(btnAssumir);
        Controls.Add(label2);
        Controls.Add(label1);
        Name = "Form1";
        Text = "Form1";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Label label2;
    private Button btnAssumir;
    private Button btnFinalizar;
    private Label lblClienteInfo;
    private ListBox lstMensagens;
    private TextBox txtMensagem;
    private Button btnEnviar;
    private ListBox lstAtendimentos;
}
