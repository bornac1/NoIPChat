using Messages;
using System.IO.IsolatedStorage;
using System.Text;

namespace Client
{
    public partial class Chat : Form
    {
        public Client client;
        private string? filepath = null;
        public Chat(Main main)
        {
            InitializeComponent();
            client = main.client;
        }
        private async Task SendMessage()
        {
            byte[]? data = await FiletoData();
            bool? isfile = null;
            if(data != null)
            {
                isfile = true;
            }
            Messages.Message message = new()
            {
                CV = client.CV,
                Sender = client.Username,
                Receiver = receivers.Text.Trim(),
                Msg = Encoding.UTF8.GetBytes(this.message.Text),
                IsFile = isfile,
                Data = data
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

        private async void Sendbutton_Click(object sender, EventArgs e)
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

        private void Cancelbutton_Click(object sender, EventArgs e)
        {
            ReturnDefault();
        }

        private void Refreshbutton_Click(object sender, EventArgs e)
        {
            display.Update();
        }

        private async void Chat_FormClosed(object sender, FormClosedEventArgs e)
        {
            await client.Disconnect(true);
        }

        private void Chat_Load(object sender, EventArgs e)
        {
            client.ischatready.TrySetResult(true);
        }

        private void Filebutton_Click(object sender, EventArgs e)
        {
            if (openfiledialog.ShowDialog() == DialogResult.OK)
            {
                filepath = openfiledialog.FileName;
                filenamelabel.Text = Path.GetFileName(filepath);
            }
        }
        private async Task<byte[]?> FiletoData()
        {
            if (filepath != null)
            {
                try
                {
                    string name = filenamelabel.Text;
                    Messages.File file = new()
                    {
                        Name = name,
                        Content = await System.IO.File.ReadAllBytesAsync(filepath)
                    };
                    return await Processing.SerializeFile(file);
                }
                catch (Exception ex)
                {
                    //TODO: error handling
                    MessageBox.Show("File open error." + ex.ToString());
                }
                finally
                {
                    filenamelabel.Text = string.Empty;
                    filepath = null;
                }
            }
            return null;
        }
        public async Task SaveFile(byte[]? data)
        {
            _ = savefiledialog.ShowDialog();
            if (data != null)
            {
                try
                {
                    Messages.File file = await Processing.DeserializeFile(data);
                    if (file.Name != null && file.Content != null)
                    {
                        string path = savefiledialog.FileName;
                        if (path == string.Empty)
                        {
                            //Default storage location
                            path = Path.Combine("Data", file.Name);
                            Directory.CreateDirectory("Data");
                        }
                        await System.IO.File.WriteAllBytesAsync(path, file.Content);
                    }
                }
                catch (Exception ex)
                {
                    //TODO: error handling
                    MessageBox.Show("File save error." + ex.ToString());
                }
            }
        }
        public void DisplayMessage(Messages.Message message)
        {
            if (message.Msg != null)
            {
                string current = display.Text;
                if (current == string.Empty)
                {
                    display.Text = $"{message.Sender}:{Encoding.UTF8.GetString(message.Msg)}";
                }
                else
                {
                    display.Text = string.Join(Environment.NewLine, current, $"{message.Sender}:{Encoding.UTF8.GetString(message.Msg)}");
                }
            }
        }
    }
}
