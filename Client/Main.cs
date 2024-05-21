using System.Diagnostics;
using System.Reflection;

namespace Client
{
    /// <summary>
    /// Main form.
    /// </summary>
    public partial class Main : Form
    {
        /// <summary>
        /// Client object.
        /// </summary>
        public Client client;
        /// <summary>
        /// Login form.
        /// </summary>
        public Login? login;
        /// <summary>
        /// Chat form.
        /// </summary>
        public Chat? chat;
        /// <summary>
        /// ServersForm form.
        /// </summary>
        public ServersForm? serversform;
        /// <summary>
        /// Files form.
        /// </summary>
        public Files? files;
        /// <summary>
        /// Main constructor.
        /// </summary>
        public Main()
        {
            InitializeComponent();
            ManipulateMenue(false);
            client = new Client(this);
        }
        private void LoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartLogin();
        }
        /// <summary>
        /// Starts chat.
        /// </summary>
        public void StartChat()
        {
            chat = new Chat(this)
            {
                MdiParent = this
            };
            chat.Show();
        }
        /// <summary>
        /// Starts login.
        /// </summary>
        public void StartLogin()
        {
            login = new Login(this)
            {
                MdiParent = this
            };
            login.Show();
        }
        /// <summary>
        /// Changes main menue based on logedin.
        /// </summary>
        /// <param name="logedin">True to enable disconnect, false to enable login.</param>
        public void ManipulateMenue(bool logedin = false)
        {
            //Change mainmenue items based on login state
            if (logedin)
            {
                loginToolStripMenuItem.Visible = false;
                disconnectToolStripMenuItem.Visible = true;
            }
            else
            {
                loginToolStripMenuItem.Visible = true;
                disconnectToolStripMenuItem.Visible = false;
            }
        }
        /// <summary>
        /// Clean up after disconnect.
        /// </summary>
        /// <param name="force">True if forced, false if connection failed.</param>
        public void CloseDisconnect(bool force = false)
        {
            if (!force)
            {
                MessageBox.Show("Connection failed.");
            }
            if (chat != null)
            {
                chat.Close();
                chat.Dispose();
            }
            if (login != null)
            {
                login.Close();
                login.Dispose();
            }
            ManipulateMenue();
        }

        private async void DisconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Messages.Message message = new()
            {
                CV = client.CV,
                Disconnect = true
            };
            await client.SendMessage(message);
            await client.Disconnect(true);
            CloseDisconnect(true);
        }
        private void KnownServersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (serversform != null)
            {
                serversform.Close();
                serversform.Dispose();
            }
            serversform = new ServersForm(this)
            {
                MdiParent = this
            };
            serversform.Show();

        }

        private void SavedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (files != null)
            {
                files.Close();
                files.Dispose();
            }
            files = new Files()
            {
                MdiParent = this
            };
            files.Show();
        }

        private async void PatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using OpenFileDialog dialog = new();
                dialog.Title = "Open patch package";
                dialog.Filter = "NoIPChat packet (*.nip)|*.nip";
                dialog.Multiselect = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(dialog.FileName))
                    {
                        string path = Path.GetFullPath(dialog.FileName);
                        await client.LoadPatch(path);
                    }
                }
            }
            catch (Exception ex)
            {
                await client.WriteLog(ex);
            }
        }
        private async void LoadUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists("Update") && Directory.GetFiles("Update").Length >0)
                {
                    //update is already prepared
                    MessageBox.Show("Client will restart.");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "Updater.exe",
                        Arguments = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + " " + "client"
                    });
                    Environment.Exit(0);
                }
                else
                {
                    using OpenFileDialog dialog = new();
                    dialog.Title = "Open update package";
                    dialog.Filter = "NoIPChat packet (*.nip)|*.nip";
                    dialog.Multiselect = false;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        if (string.IsNullOrEmpty(dialog.FileName))
                        {
                            string path = Path.GetFullPath(dialog.FileName);
                            client.PrepareUpdate(path);
                            MessageBox.Show("Client will restart.");
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "Updater.exe",
                                Arguments = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + " " + "client"
                            });
                            Environment.Exit(0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await client.WriteLog(ex);
            }
        }

        private async void RequestUpdateFromServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await client.RequestUpdate();
                MessageBox.Show("Update requested.");
            } catch (Exception ex)
            {
                await client.WriteLog(ex);
            }
        }
    }
}
