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
            button1 = new Button();
            button2 = new Button();
            SuspendLayout();
            // 
            // display
            // 
            display.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            display.Location = new Point(12, 43);
            display.Name = "display";
            display.ReadOnly = true;
            display.Size = new Size(949, 217);
            display.TabIndex = 0;
            display.Text = "";
            // 
            // message
            // 
            message.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            message.Location = new Point(92, 347);
            message.Name = "message";
            message.Size = new Size(869, 110);
            message.TabIndex = 2;
            message.Text = "";
            // 
            // receivers
            // 
            receivers.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            receivers.Location = new Point(92, 293);
            receivers.Name = "receivers";
            receivers.Size = new Size(869, 48);
            receivers.TabIndex = 1;
            receivers.Text = "";
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Location = new Point(12, 296);
            label1.Name = "label1";
            label1.Size = new Size(74, 20);
            label1.TabIndex = 1;
            label1.Text = "Receivers:";
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            label2.AutoSize = true;
            label2.Location = new Point(16, 392);
            label2.Name = "label2";
            label2.Size = new Size(70, 20);
            label2.TabIndex = 3;
            label2.Text = "Message:";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            button1.Location = new Point(92, 463);
            button1.Name = "button1";
            button1.Size = new Size(94, 29);
            button1.TabIndex = 3;
            button1.Text = "Send";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button2.Location = new Point(867, 463);
            button2.Name = "button2";
            button2.Size = new Size(94, 29);
            button2.TabIndex = 4;
            button2.Text = "Cancel";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // Chat
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(973, 505);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(receivers);
            Controls.Add(message);
            Controls.Add(display);
            Name = "Chat";
            Text = "Chat";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RichTextBox message;
        private RichTextBox receivers;
        private Label label1;
        private Label label2;
        private Button button1;
        private Button button2;
        public RichTextBox display;
    }
}