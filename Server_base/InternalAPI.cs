using Messages;
using System.Net;
using Transport;

namespace Server
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
        private readonly string username;
        private readonly string password;
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
                    connected = false;
                }
            }
        }
        private async Task<int> ReadLength()
        {
            int totalread = 0;
            int offset = 0;
            while (totalread < bufferl.Length)
            {
                if (client != null)
                {
                    int read = await client.ReceiveAsync(new Memory<byte>(bufferl, offset, bufferl.Length - totalread));
                    totalread += read;
                    offset += read;
                }
            }
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bufferl, 0));
        }
        private async Task<byte[]> ReadData(int length)
        {
            byte[] buffer = new byte[length];
            int totalread = 0;
            int offset = 0;
            while (totalread < buffer.Length)
            {
                if (client != null)
                {
                    int read = await client.ReceiveAsync(new Memory<byte>(buffer, offset, buffer.Length - totalread));
                    totalread += read;
                    offset += read;
                }
            }
            return buffer;
        }
        private async Task ProcessMessage(APIMessage message)
        {
            if (message.Command != null)
            {
                if (message.Command == "login")
                {
                    bool auth;
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
                    await client.SendAsync(length);
                    await client.SendAsync(data);
                    return true;
                }
                return false;

            }
            catch
            {
                connected = false;
                return false;
            }
        }
    }
}
