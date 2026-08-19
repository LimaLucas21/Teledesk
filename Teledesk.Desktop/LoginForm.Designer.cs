namespace Teledesk.Desktop
{
    partial class LoginForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Login = new Label();
            txtEmail = new TextBox();
            lbSenha = new Label();
            txtSenha = new TextBox();
            btnEntrar = new Button();
            SuspendLayout();
            // 
            // Login
            // 
            Login.AutoSize = true;
            Login.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            Login.Location = new Point(184, 60);
            Login.Name = "Login";
            Login.Size = new Size(74, 32);
            Login.TabIndex = 0;
            Login.Text = "Login";
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(123, 127);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(195, 23);
            txtEmail.TabIndex = 1;
            // 
            // lbSenha
            // 
            lbSenha.AutoSize = true;
            lbSenha.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lbSenha.Location = new Point(180, 187);
            lbSenha.Name = "lbSenha";
            lbSenha.Size = new Size(80, 32);
            lbSenha.TabIndex = 2;
            lbSenha.Text = "Senha";
            // 
            // txtSenha
            // 
            txtSenha.Location = new Point(119, 251);
            txtSenha.Name = "txtSenha";
            txtSenha.Size = new Size(195, 23);
            txtSenha.TabIndex = 3;
            // 
            // btnEntrar
            // 
            btnEntrar.Location = new Point(168, 321);
            btnEntrar.Name = "btnEntrar";
            btnEntrar.Size = new Size(111, 23);
            btnEntrar.TabIndex = 4;
            btnEntrar.Text = "Entrar";
            btnEntrar.UseVisualStyleBackColor = true;
            btnEntrar.Click += btnEntrar_Click;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(433, 450);
            Controls.Add(btnEntrar);
            Controls.Add(txtSenha);
            Controls.Add(lbSenha);
            Controls.Add(txtEmail);
            Controls.Add(Login);
            Name = "LoginForm";
            Text = "LoginForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label Login;
        private TextBox txtEmail;
        private Label lbSenha;
        private TextBox txtSenha;
        private Button btnEntrar;
    }
}