namespace Client
{
    public partial class Main : Form
    {
        public Client client;
        public Login? login;
        public Chat? chat;
        public ServersForm? serversform;
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
        public void StartChat()
        {
            chat = new Chat(this)
            {
                MdiParent = this
            };
            chat.Show();
        }
        public void StartLogin()
        {
            login = new Login(this)
            {
                MdiParent = this
            };
            login.Show();
        }
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
            client = new Client(this);
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
            serversform = new ServersForm(this)
            {
                MdiParent = this
            };
            serversform.Show();

        }
    }
}
