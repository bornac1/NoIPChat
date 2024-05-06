using System.Net;
using Messages;
using Transport;

namespace ServerAPI
{
    public class API
    {
        //This API is used to interact with Server instance already running in separate process or on separate machine.
        private readonly TcpClient? client;
        private readonly string IP;
        private readonly int port;
        private bool connected = false;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        private byte[] bufferm = new byte[1024];
        private bool auth = false;
        private readonly LogCallback? logcallback;
        private readonly LogCallbackAsync? logcallbackasync;

        public delegate void LogCallback(string message);
        public delegate Task LogCallbackAsync(string message);
        private API(string localIP, string IP, int port, LogCallback? logcallback, LogCallbackAsync? logcallbackasync)
        {
            client = new(new IPEndPoint(IPAddress.Parse(localIP), 0));
            this.IP = IP;
            this.port = port;
            this.logcallback = logcallback;
            this.logcallbackasync = logcallbackasync;
        }
        /// <summary>
        /// Initializes Server API.
        /// This API is used to interact with Server instance already running in separate process or on separate machine.
        /// </summary>
        public static async Task<API?> CreateAPI(string localIP, string IP, int port, LogCallback? logcallback, LogCallbackAsync? logcallbackasync)
        {
            API? api = null;
            try
            {
                api = new(localIP, IP, port, logcallback, logcallbackasync);
                if (api.client != null)
                {
                    await api.client.ConnectAsync(IPAddress.Parse(IP), port);
                    api.connected = true;
                }

            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //Connection error
                    api?.Disconnect();
                }
                else
                {
                    //Logging
                }
            }
            return api;
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
        public async Task Login(string username, string password)
        {
            username = username.ToLower();
            await SendMessage(new()
            {
                Command = "login",
                Username = username,
                Password = password
            });
            _ = Receive();
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
        private async Task ProcessMessage(APIMessage message)
        {
            if (message.Auth != null)
            {
                auth = (bool)message.Auth;
            }
            if (auth)
            {
                if (message.Command != null)
                {
                    if (message.Command == "log")
                    {
                        await ProcessLogMessage(message);
                    }
                }
            }
        }
        private async Task ProcessLogMessage(APIMessage message)
        {
            if (message.Message != null)
            {
                //Do something with log message
                logcallback?.Invoke(message.Message);
                if (logcallbackasync != null)
                {
                    await logcallbackasync(message.Message);
                }
            }
        }
        private async Task Receive()
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
        private void Disconnect()
        {
            connected = false;
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
        }
    }
}
