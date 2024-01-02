namespace Client
{
    partial class Chat
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
            display = new RichTextBox();
            message = new RichTextBox();
            receivers = new RichTextBox();
            label1 = new Label();
            label2 = new Label();
            sendbutton = new Button();
            cancelbutton = new Button();
            refreshbutton = new Button();
            openfiledialog = new OpenFileDialog();
            filebutton = new Button();
            filenamelabel = new Label();
            savefiledialog = new SaveFileDialog();
            SuspendLayout();
            // 
            // display
            // 
            display.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            display.Location = new Point(10, 32);
            display.Margin = new Padding(3, 2, 3, 2);
            display.Name = "display";
            display.ReadOnly = true;
            display.Size = new Size(831, 164);
            display.TabIndex = 0;
            display.Text = "";
            // 
            // message
            // 
            message.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            message.Location = new Point(80, 260);
            message.Margin = new Padding(3, 2, 3, 2);
            message.Name = "message";
            message.Size = new Size(761, 84);
            message.TabIndex = 2;
            message.Text = "";
            // 
            // receivers
            // 
            receivers.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            receivers.Location = new Point(80, 220);
            receivers.Margin = new Padding(3, 2, 3, 2);
            receivers.Name = "receivers";
            receivers.Size = new Size(761, 37);
            receivers.TabIndex = 1;
            receivers.Text = "";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Location = new Point(10, 222);
            label1.Name = "label1";
            label1.Size = new Size(59, 15);
            label1.TabIndex = 1;
            label1.Text = "Receivers:";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Location = new Point(14, 294);
            label2.Name = "label2";
            label2.Size = new Size(56, 15);
            label2.TabIndex = 3;
            label2.Text = "Message:";
            // 
            // sendbutton
            // 
            sendbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            sendbutton.Location = new Point(80, 347);
            sendbutton.Margin = new Padding(3, 2, 3, 2);
            sendbutton.Name = "sendbutton";
            sendbutton.Size = new Size(82, 22);
            sendbutton.TabIndex = 3;
            sendbutton.Text = "Send";
            sendbutton.UseVisualStyleBackColor = true;
            sendbutton.Click += Sendbutton_Click;
            // 
            // cancelbutton
            // 
            cancelbutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelbutton.Location = new Point(759, 347);
            cancelbutton.Margin = new Padding(3, 2, 3, 2);
            cancelbutton.Name = "cancelbutton";
            cancelbutton.Size = new Size(82, 22);
            cancelbutton.TabIndex = 4;
            cancelbutton.Text = "Cancel";
            cancelbutton.UseVisualStyleBackColor = true;
            cancelbutton.Click += Cancelbutton_Click;
            // 
            // refreshbutton
            // 
            refreshbutton.Location = new Point(294, 6);
            refreshbutton.Margin = new Padding(3, 2, 3, 2);
            refreshbutton.Name = "refreshbutton";
            refreshbutton.Size = new Size(82, 22);
            refreshbutton.TabIndex = 5;
            refreshbutton.Text = "Refresh";
            refreshbutton.UseVisualStyleBackColor = true;
            refreshbutton.Click += Refreshbutton_Click;
            // 
            // filebutton
            // 
            filebutton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            filebutton.Location = new Point(301, 349);
            filebutton.Name = "filebutton";
            filebutton.Size = new Size(75, 23);
            filebutton.TabIndex = 6;
            filebutton.Text = "Select file";
            filebutton.UseVisualStyleBackColor = true;
            filebutton.Click += Filebutton_Click;
            // 
            // filenamelabel
            // 
            filenamelabel.AutoSize = true;
            filenamelabel.Location = new Point(382, 353);
            filenamelabel.Name = "filenamelabel";
            filenamelabel.Size = new Size(0, 15);
            filenamelabel.TabIndex = 7;
            // 
            // Chat
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(851, 379);
            Controls.Add(filenamelabel);
            Controls.Add(filebutton);
            Controls.Add(refreshbutton);
            Controls.Add(cancelbutton);
            Controls.Add(sendbutton);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(receivers);
            Controls.Add(message);
            Controls.Add(display);
            Margin = new Padding(3, 2, 3, 2);
            Name = "Chat";
            Text = "Chat";
            FormClosed += Chat_FormClosed;
            Load += Chat_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RichTextBox message;
        private RichTextBox receivers;
        private Label label1;
        private Label label2;
        private Button sendbutton;
        private Button cancelbutton;
        public RichTextBox display;
        private Button refreshbutton;
        private OpenFileDialog openfiledialog;
        private Button filebutton;
        private Label filenamelabel;
        private SaveFileDialog savefiledialog;
    }
}