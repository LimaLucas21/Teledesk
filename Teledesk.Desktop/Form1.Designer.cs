namespace Teledesk.Desktop;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        label1 = new Label();
        label2 = new Label();
        cmbFiltroSetor = new ComboBox();
        btnGestao = new Button();
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
        label1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        label1.Location = new Point(63, 25);
        label1.Name = "label1";
        label1.Size = new Size(340, 19);
        label1.TabIndex = 0;
        label1.Text = "Teledesk WhatsApp - Painel de Atendimento";
        // 
        // btnGestao
        // 
        btnGestao.Location = new Point(680, 22);
        btnGestao.Name = "btnGestao";
        btnGestao.Size = new Size(233, 28);
        btnGestao.TabIndex = 9;
        btnGestao.Text = "Setores / Números / Simulador";
        btnGestao.UseVisualStyleBackColor = true;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(63, 82);
        label2.Name = "label2";
        label2.Size = new Size(117, 15);
        label2.TabIndex = 1;
        label2.Text = "Fila de Atendimento:";
        // 
        // cmbFiltroSetor
        // 
        cmbFiltroSetor.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFiltroSetor.FormattingEnabled = true;
        cmbFiltroSetor.Location = new Point(63, 106);
        cmbFiltroSetor.Name = "cmbFiltroSetor";
        cmbFiltroSetor.Size = new Size(265, 23);
        cmbFiltroSetor.TabIndex = 10;
        // 
        // btnAssumir
        // 
        btnAssumir.Location = new Point(84, 539);
        btnAssumir.Name = "btnAssumir";
        btnAssumir.Size = new Size(90, 28);
        btnAssumir.TabIndex = 2;
        btnAssumir.Text = "Assumir";
        btnAssumir.UseVisualStyleBackColor = true;
        // 
        // btnFinalizar
        // 
        btnFinalizar.Location = new Point(210, 539);
        btnFinalizar.Name = "btnFinalizar";
        btnFinalizar.Size = new Size(90, 28);
        btnFinalizar.TabIndex = 3;
        btnFinalizar.Text = "Finalizar";
        btnFinalizar.UseVisualStyleBackColor = true;
        // 
        // lblClienteInfo
        // 
        lblClienteInfo.AutoSize = true;
        lblClienteInfo.Location = new Point(373, 109);
        lblClienteInfo.Name = "lblClienteInfo";
        lblClienteInfo.Size = new Size(125, 15);
        lblClienteInfo.TabIndex = 4;
        lblClienteInfo.Text = "Selecione um cliente...";
        // 
        // lstMensagens
        // 
        lstMensagens.FormattingEnabled = true;
        lstMensagens.Location = new Point(373, 145);
        lstMensagens.Name = "lstMensagens";
        lstMensagens.Size = new Size(540, 368);
        lstMensagens.TabIndex = 5;
        // 
        // txtMensagem
        // 
        txtMensagem.Location = new Point(373, 540);
        txtMensagem.Name = "txtMensagem";
        txtMensagem.PlaceholderText = "Digite sua resposta para o WhatsApp...";
        txtMensagem.Size = new Size(445, 23);
        txtMensagem.TabIndex = 6;
        // 
        // btnEnviar
        // 
        btnEnviar.Location = new Point(838, 539);
        btnEnviar.Name = "btnEnviar";
        btnEnviar.Size = new Size(75, 28);
        btnEnviar.TabIndex = 7;
        btnEnviar.Text = "Enviar";
        btnEnviar.UseVisualStyleBackColor = true;
        // 
        // lstAtendimentos
        // 
        lstAtendimentos.FormattingEnabled = true;
        lstAtendimentos.Location = new Point(63, 145);
        lstAtendimentos.Name = "lstAtendimentos";
        lstAtendimentos.Size = new Size(265, 368);
        lstAtendimentos.TabIndex = 8;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 621);
        Controls.Add(cmbFiltroSetor);
        Controls.Add(btnGestao);
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
        Text = "Teledesk WhatsApp";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label label1;
    private Label label2;
    private ComboBox cmbFiltroSetor;
    private Button btnGestao;
    private Button btnAssumir;
    private Button btnFinalizar;
    private Label lblClienteInfo;
    private ListBox lstMensagens;
    private TextBox txtMensagem;
    private Button btnEnviar;
    private ListBox lstAtendimentos;
}
