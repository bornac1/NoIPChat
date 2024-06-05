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
            patchToolStripMenuItem = new ToolStripMenuItem();
            updateToolStripMenuItem = new ToolStripMenuItem();
            requestUpdateFromServerToolStripMenuItem = new ToolStripMenuItem();
            loadUpdateToolStripMenuItem = new ToolStripMenuItem();
            mainmenu.SuspendLayout();
            SuspendLayout();
            // 
            // mainmenu
            // 
            mainmenu.ImageScalingSize = new Size(20, 20);
            mainmenu.Items.AddRange(new ToolStripItem[] { loginToolStripMenuItem, disconnectToolStripMenuItem, settingsToolStripMenuItem });
            mainmenu.Location = new Point(0, 0);
            mainmenu.Name = "mainmenu";
            mainmenu.Padding = new Padding(6, 3, 0, 3);
            mainmenu.Size = new Size(800, 30);
            mainmenu.TabIndex = 1;
            mainmenu.Text = "menuStrip1";
            // 
            // loginToolStripMenuItem
            // 
            loginToolStripMenuItem.Name = "loginToolStripMenuItem";
            loginToolStripMenuItem.Size = new Size(60, 24);
            loginToolStripMenuItem.Text = "Login";
            loginToolStripMenuItem.Click += LoginToolStripMenuItem_Click;
            // 
            // disconnectToolStripMenuItem
            // 
            disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            disconnectToolStripMenuItem.Size = new Size(96, 24);
            disconnectToolStripMenuItem.Text = "Disconnect";
            disconnectToolStripMenuItem.Click += DisconnectToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { knownServersToolStripMenuItem, savedFilesToolStripMenuItem, patchToolStripMenuItem, updateToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(76, 24);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // knownServersToolStripMenuItem
            // 
            knownServersToolStripMenuItem.Name = "knownServersToolStripMenuItem";
            knownServersToolStripMenuItem.Size = new Size(224, 26);
            knownServersToolStripMenuItem.Text = "Known servers";
            knownServersToolStripMenuItem.Click += KnownServersToolStripMenuItem_Click;
            // 
            // savedFilesToolStripMenuItem
            // 
            savedFilesToolStripMenuItem.Name = "savedFilesToolStripMenuItem";
            savedFilesToolStripMenuItem.Size = new Size(224, 26);
            savedFilesToolStripMenuItem.Text = "Saved files";
            savedFilesToolStripMenuItem.Click += SavedFilesToolStripMenuItem_Click;
            // 
            // patchToolStripMenuItem
            // 
            patchToolStripMenuItem.Name = "patchToolStripMenuItem";
            patchToolStripMenuItem.Size = new Size(224, 26);
            patchToolStripMenuItem.Text = "Patch";
            patchToolStripMenuItem.Click += PatchToolStripMenuItem_Click;
            // 
            // updateToolStripMenuItem
            // 
            updateToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { requestUpdateFromServerToolStripMenuItem, loadUpdateToolStripMenuItem });
            updateToolStripMenuItem.Name = "updateToolStripMenuItem";
            updateToolStripMenuItem.Size = new Size(224, 26);
            updateToolStripMenuItem.Text = "Update";
            // 
            // requestUpdateFromServerToolStripMenuItem
            // 
            requestUpdateFromServerToolStripMenuItem.Name = "requestUpdateFromServerToolStripMenuItem";
            requestUpdateFromServerToolStripMenuItem.Size = new Size(277, 26);
            requestUpdateFromServerToolStripMenuItem.Text = "Request update from Server";
            requestUpdateFromServerToolStripMenuItem.Click += RequestUpdateFromServerToolStripMenuItem_Click;
            // 
            // loadUpdateToolStripMenuItem
            // 
            loadUpdateToolStripMenuItem.Name = "loadUpdateToolStripMenuItem";
            loadUpdateToolStripMenuItem.Size = new Size(277, 26);
            loadUpdateToolStripMenuItem.Text = "Load update";
            loadUpdateToolStripMenuItem.Click += LoadUpdateToolStripMenuItem_Click;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 451);
            Controls.Add(mainmenu);
            IsMdiContainer = true;
            MainMenuStrip = mainmenu;
            Name = "Main";
            Text = "Main";
            WindowState = FormWindowState.Maximized;
            mainmenu.ResumeLayout(false);
            mainmenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ToolStripMenuItem loginToolStripMenuItem;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem disconnectToolStripMenuItem;
        private ToolStripMenuItem knownServersToolStripMenuItem;
        private ToolStripMenuItem savedFilesToolStripMenuItem;
        /// <summary>
        /// Main menu.
        /// </summary>
        public MenuStrip mainmenu;
        private ToolStripMenuItem patchToolStripMenuItem;
        private ToolStripMenuItem updateToolStripMenuItem;
        private ToolStripMenuItem requestUpdateFromServerToolStripMenuItem;
        private ToolStripMenuItem loadUpdateToolStripMenuItem;
    }
}