namespace Client
{
    partial class Login
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
            label1 = new Label();
            label2 = new Label();
            username = new TextBox();
            password = new TextBox();
            Login_button = new Button();
            label3 = new Label();
            server = new ComboBox();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(17, 86);
            label1.Name = "label1";
            label1.Size = new Size(75, 20);
            label1.TabIndex = 0;
            label1.Text = "Username";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(22, 123);
            label2.Name = "label2";
            label2.Size = new Size(70, 20);
            label2.TabIndex = 1;
            label2.Text = "Password";
            // 
            // username
            // 
            username.Location = new Point(92, 83);
            username.Name = "username";
            username.Size = new Size(253, 27);
            username.TabIndex = 0;
            // 
            // password
            // 
            password.Location = new Point(92, 116);
            password.Name = "password";
            password.Size = new Size(253, 27);
            password.TabIndex = 1;
            password.UseSystemPasswordChar = true;
            // 
            // Login_button
            // 
            Login_button.Location = new Point(42, 183);
            Login_button.Name = "Login_button";
            Login_button.Size = new Size(94, 29);
            Login_button.TabIndex = 3;
            Login_button.Text = "Login";
            Login_button.UseVisualStyleBackColor = true;
            Login_button.Click += Login_button_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(42, 152);
            label3.Name = "label3";
            label3.Size = new Size(50, 20);
            label3.TabIndex = 5;
            label3.Text = "Server";
            // 
            // server
            // 
            server.FormattingEnabled = true;
            server.Location = new Point(92, 149);
            server.Name = "server";
            server.Size = new Size(253, 28);
            server.TabIndex = 2;
            server.DisplayMember = "Key";
            server.ValueMember = "Value";
            // 
            // Login
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(357, 221);
            Controls.Add(server);
            Controls.Add(label3);
            Controls.Add(Login_button);
            Controls.Add(password);
            Controls.Add(username);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "Login";
            Text = "Login";
            Load += Login_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox username;
        private TextBox password;
        private Button Login_button;
        private Label label3;
        private ComboBox server;
    }
}