namespace Client
{
    /// <summary>
    /// Login form.
    /// </summary>
    public partial class Login : Form
    {
        private readonly Client client;
        private bool loginclicked;
        private readonly Main main;
        /// <summary>
        /// Login constructor.
        /// </summary>
        /// <param name="main"></param>
        public Login(Main main)
        {
            this.main = main;
            client = main.client;
            InitializeComponent();
            server.DisplayMember = "Name";
            server.ValueMember = "Servers";
            server.DataSource = client.servers;
            loginclicked = false;
        }

        private async void Login_button_Click(object sender, EventArgs e)
        {
            if (!loginclicked)
            {
                if (server.SelectedValue != null)
                {
                    await client.Connect((Servers)server.SelectedValue);
                    if (client.connected)
                    //Connected to server
                    {
                        await client.Login(username.Text, password.Text);
                        loginclicked = true;
                        if (await client.auth.Task == true)
                        {
                            //Is authenticated
                            MessageBox.Show("Authenticated");
                            main.ManipulateMenue(true);
                            main.StartChat();
                            Close();
                            Dispose();
                        }
                        else
                        {
                            //Not authenticated
                            loginclicked = false;
                            main.ManipulateMenue(false);
                            MessageBox.Show("Authentication error");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Processing login.");
                    }
                }
                else
                {
                    MessageBox.Show("Error connecting to the server.");
                }
            }
        }
        /*private async Task WaitForAuth()
        {
            while (client.auth == null)
            {
                await Task.Delay(1);
            }
        }*/

        private void Login_Load(object sender, EventArgs e)
        {
        }
    }
}
