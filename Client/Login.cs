﻿namespace Client
{
    public partial class Login : Form
    {
        public Client client;
        private bool loginclicked;
        private readonly Main main;
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
            if (!loginclicked && client.auth != true)
            {
                if (server.SelectedValue != null)
                {
                    await client.Connect((Servers)server.SelectedValue);
                    if (client.connected)
                    //Connected to server
                    {
                        await client.Login(username.Text, password.Text);
                        loginclicked = true;
                        await WaitForAuth();
                        if (client.auth == true)
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
        private async Task WaitForAuth()
        {
            while (client.auth == null)
            {
                await Task.Delay(10);
            }
        }

        private void Login_Load(object sender, EventArgs e)
        {
        }
    }
}
