namespace Client
{
    public partial class Chat : Form
    {
        public Client client;
        public Chat(Main main)
        {
            InitializeComponent();
            client = main.client;
        }
        private async Task SendMessage()
        {
            //Enter only is used to send
            //Shift+Entre is for new line
            Messages.Message message = new()
            {
                CV = client.CV,
                Sender = client.Username,
                Receiver = receivers.Text.Trim(),
                Msg = this.message.Text
            };
            if (await client.SendMessage(message))
            {
                //empty after sending
                ReturnDefault();
            }
            else
            {
                //Error sending message
                //Let's save it
                MessageBox.Show("Message not sent.");
            }
        }
        private void ReturnDefault()
        {
            receivers.Text = string.Empty;
            message.Text = string.Empty;
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            if (receivers.Text != string.Empty && message.Text != string.Empty)
            {
                await SendMessage();
            }
            else
            {
                ReturnDefault();
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            ReturnDefault();
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            display.Update();
        }

        private async void Chat_FormClosed(object sender, FormClosedEventArgs e)
        {
            await client.Disconnect(true);
        }

        private void Chat_Load(object sender, EventArgs e)
        {
            client.ischatready = true;
        }
    }
}
