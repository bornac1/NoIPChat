namespace Client
{
    partial class Main
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
            mainmenu = new MenuStrip();
            loginToolStripMenuItem = new ToolStripMenuItem();
            disconnectToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            knownServersToolStripMenuItem = new ToolStripMenuItem();
            savedFilesToolStripMenuItem = new ToolStripMenuItem();
            mainmenu.SuspendLayout();
            SuspendLayout();
            // 
            // mainmenu
            // 
            mainmenu.ImageScalingSize = new Size(20, 20);
            mainmenu.Items.AddRange(new ToolStripItem[] { loginToolStripMenuItem, disconnectToolStripMenuItem, settingsToolStripMenuItem });
            mainmenu.Location = new Point(0, 0);
            mainmenu.Name = "mainmenu";
            mainmenu.Padding = new Padding(5, 2, 0, 2);
            mainmenu.Size = new Size(700, 24);
            mainmenu.TabIndex = 1;
            mainmenu.Text = "menuStrip1";
            // 
            // loginToolStripMenuItem
            // 
            loginToolStripMenuItem.Name = "loginToolStripMenuItem";
            loginToolStripMenuItem.Size = new Size(49, 20);
            loginToolStripMenuItem.Text = "Login";
            loginToolStripMenuItem.Click += LoginToolStripMenuItem_Click;
            // 
            // disconnectToolStripMenuItem
            // 
            disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            disconnectToolStripMenuItem.Size = new Size(78, 20);
            disconnectToolStripMenuItem.Text = "Disconnect";
            disconnectToolStripMenuItem.Click += DisconnectToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { knownServersToolStripMenuItem, savedFilesToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(61, 20);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // knownServersToolStripMenuItem
            // 
            knownServersToolStripMenuItem.Name = "knownServersToolStripMenuItem";
            knownServersToolStripMenuItem.Size = new Size(180, 22);
            knownServersToolStripMenuItem.Text = "Known servers";
            knownServersToolStripMenuItem.Click += KnownServersToolStripMenuItem_Click;
            // 
            // savedFilesToolStripMenuItem
            // 
            savedFilesToolStripMenuItem.Name = "savedFilesToolStripMenuItem";
            savedFilesToolStripMenuItem.Size = new Size(180, 22);
            savedFilesToolStripMenuItem.Text = "Saved files";
            savedFilesToolStripMenuItem.Click += savedFilesToolStripMenuItem_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(700, 338);
            Controls.Add(mainmenu);
            IsMdiContainer = true;
            MainMenuStrip = mainmenu;
            Margin = new Padding(3, 2, 3, 2);
            Name = "Main";
            Text = "Main";
            WindowState = FormWindowState.Maximized;
            mainmenu.ResumeLayout(false);
            mainmenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip mainmenu;
        private ToolStripMenuItem loginToolStripMenuItem;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripMenuItem knownServersToolStripMenuItem;
        private ToolStripMenuItem savedFilesToolStripMenuItem;
    }
}