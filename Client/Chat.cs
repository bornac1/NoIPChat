using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Messages;
using static System.Net.Mime.MediaTypeNames;

namespace Client
{
    public partial class Chat : Form
    {
        public Client client;
        public Chat(Main main)
        {
            InitializeComponent();
            client = main.client;
            client.ischatready = true;
           _ =  client.PrintReceivedMessages();
        }
        private async Task SendMessage()
        {
            //Enter only is used to send
            //Shift+Entre is for new line
            Messages.Message message = new()
            {
                CV = client.CV,
                Sender = client.Username,
                Receiver = receivers.Text,
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
    }
}
