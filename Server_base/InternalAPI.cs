using System.Net;
using Messages;
using Transport;

namespace Server_base
{
    public class Remote
    {
        //Internal part of API used for communication to other processes
        //Used for remote controling of the Server
        private readonly TcpListener listener;
        private bool active = true;
        private TcpClient? client;
        private bool connected = false;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        private byte[] bufferm = new byte[1024];
        private readonly string username;
        private readonly string password;
        private bool auth = false;
        public Remote(string IP, int port, string username, string password)
        {
            listener = new(System.Net.IPAddress.Parse(IP), port);
            this.username = username;
            this.password = password;
            listener.Start();
            _ = Accept();
        }
        public void Close()
        {
            active = false;
            listener.Stop();
            listener.Dispose();
        }
        public async Task Accept()
        {
            while (active)
            {
                if (client != null && !connected)
                {
                    client.Close();
                    client.Dispose();
                }
                client = await listener.AcceptAsync();
                connected = true;
                await HandleClient();
            }
        }
        public async Task HandleClient()
        {
            while (connected)
            {
                try
                {
                    int length = await ReadLength();
                    await ProcessMessage(await Processing.DeserializeAPI(await ReadData(length)));
                }
                catch
                {
                    Disconnect();
                }
            }
        }
        private async Task<int> ReadLength()
        {
            int totalread = 0;
            int offset = 0;
            if (client != null)
            {
                while (totalread < bufferl.Length)
                {
                    int read = await client.ReceiveAsync(new Memory<byte>(bufferl, offset, bufferl.Length - totalread));
                    totalread += read;
                    offset += read;
                }
            }
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bufferl, 0));
        }
        private void Handlebufferm(int size)
        {
            if (bufferm.Length >= size)
            {
                //Buffer is large enough
                if (bufferm.Length / size >= 2)
                {
                    //Buffer is at least 2 times too large
                    int ns = bufferm.Length / (bufferm.Length / size);
                    bufferm = new byte[ns];
                }
            }
            else
            {
                //Buffer is too small
                int ns = size / bufferm.Length;
                if (size % bufferm.Length != 0)
                {
                    ns += 1;
                }
                ns *= bufferm.Length;
                bufferm = new byte[ns];
            }
        }
        private async Task<ReadOnlyMemory<byte>> ReadData(int length)
        {
            Handlebufferm(length);
            int totalread = 0;
            if (client != null)
            {
                while (totalread < length)
                {
                    int read = await client.ReceiveAsync(new Memory<byte>(bufferm, totalread, length - totalread));
                    totalread += read;
                }
            }
            return new ReadOnlyMemory<byte>(bufferm, 0, totalread);
        }
        private async Task ProcessMessage(APIMessage message)
        {
            if (message.Command != null)
            {
                if (message.Command == "login")
                {
                    if (message.Username == username && message.Password == password)
                    {
                        auth = true;
                    }
                    else
                    {
                        auth = false;
                    }
                    await SendMessage(new APIMessage()
                    {
                        Auth = auth
                    });
                }
            }
        }
        private async Task<bool> SendMessage(APIMessage message)
        {
            try
            {
                if (client != null)
                {
                    byte[] data = await Processing.SerializeAPI(message);
                    byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
                    int sent = 0;
                    while (sent < length.Length)
                    {
                        await client.SendAsync(new ReadOnlyMemory<byte>(length, sent, length.Length - sent));
                    }
                    sent = 0;
                    while (sent < data.Length)
                    {
                        await client.SendAsync(new ReadOnlyMemory<byte>(data, sent, data.Length - sent));
                    }
                    return true;
                }
                return false;

            }
            catch
            {
                Disconnect();
                return false;
            }
        }
        private void Disconnect()
        {
            connected = false;
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
        }
        public async Task SendLog(string message)
        {
            if (auth)
            {
                await SendMessage(new()
                {
                    Command = "log",
                    Message = message
                });
            }
        }
    }
}
