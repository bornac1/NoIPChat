using Messages;
using Sodium;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Transport;

namespace Client
{
    public class Client
    {
        public int CV = 1;
        public bool connected = false;
        public ConcurrentQueue<Messages.Message> messages_snd;
        public string? Username;
        public string? Password;
        public Servers? Server;
        public TClient client;
        public BindingSource servers;
        private readonly StringBuilder value;
        public TaskCompletionSource<bool> ischatready = new();
        public Main main;
        private bool disconnectstarted;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        private byte[] bufferm = new byte[1024];
        private readonly KeyPair my;
        private byte[]? aeskey;
        public TaskCompletionSource<bool> auth = new();
        private readonly System.Timers.Timer ReconnectTimer;
        private const double ReconnectTimeOut = 60000;//60 seconds
        private const double InitialReconnectInterval = 15;
        public Client(Main main)
        {
            this.main = main;
            disconnectstarted = false;
            ReconnectTimer = new(InitialReconnectInterval);
            ReconnectTimer.Elapsed += Reconnect;
            messages_snd = [];
            servers = [];
            value = new StringBuilder();
            my = Encryption.GenerateECDH();
            _ = LoadServers();
            client = new TClient(new TcpClient(new IPEndPoint(IPAddress.Any, 0)));
        }
        public async Task Connect(Servers srv)
        {
            Server = srv;
            try
            {
                client = new TClient(new TcpClient(new IPEndPoint(IPAddress.Any, 0)));
                await client.ConnectAsync(IPAddress.Parse(srv.IP), srv.Port);
                connected = true;
                //Send public key message
                await SendMessage(new Messages.Message()
                {
                    PublicKey = my.PublicKey
                });
                await ReceiveKey();
            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //Error connecting
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
                //Clean all
                await Disconnect();
            }
        }
        private async Task ReceiveKey()
        {
            try
            {
                int length = await ReadLength();
                ReadOnlyMemory<byte> data = await ReadData(length);
                Messages.Message message = await Processing.Deserialize(data);
                await ProcessMessage(message);
            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //assume disconnection
                    await Disconnect();
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                    //Clean all
                    await Disconnect();
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
                    ReadOnlyMemory<byte> data = await ReadData(length);
                    Messages.Message message = await Processing.Deserialize(data);
                    await ProcessMessage(message);
                }
                catch (Exception ex)
                {
                    if (ex is TransportException)
                    {
                        //assume disconnection
                    }
                    else if (ex is ObjectDisposedException)
                    {
                        //Already disposed
                    }
                    else
                    {
                        //Logging
                        await WriteLog(ex);
                    }
                    //Clean all
                    await Disconnect();
                }
            }
        }
        private async Task<int> ReadLength()
        {
            int totalread = 0;
            while (totalread < bufferl.Length)
            {
                int read = await client.ReceiveAsync(bufferl, totalread, bufferl.Length - totalread);
                totalread += read;
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
            while (totalread < length)
            {
                int read = await client.ReceiveAsync(bufferm, totalread, length - totalread);
                totalread += read;
            }
            return new ReadOnlyMemory<byte>(bufferm, 0, totalread);
        }
        public async Task Login(string username, string password)
        {
            Messages.Message message = new();
            Username = username.ToLower();
            Password = password;
            message.CV = CV;
            message.User = Username;
            message.Pass = Encoding.UTF8.GetBytes(Password);
            if (await SendMessage(message))
            {
                _ = Receive();
            }
        }
        public async Task<bool> SendMessage(Messages.Message message)
        {
            bool msgerror = false;
            try
            {
                //Encrypt message
                if (aeskey != null)
                {
                    message = Encryption.EncryptMessage(message, aeskey);
                }
                byte[]? data = await Processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
                    await client.SendAsync(length);
                    await client.SendAsync(data);
                    return true;
                }
                else
                {
                    //Message error
                    msgerror = true;
                }
            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //Assume disconnection
                    //Save message to be sent later
                    if (!msgerror)
                    {
                        messages_snd.Enqueue(message);
                    }
                    else
                    {
                        //Message error
                        //Give up
                    }
                }
                else if (ex is ObjectDisposedException)
                {
                    //Already disposed
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
            }
            await Disconnect();
            return false;
        }
        public (string?, int) GetServer(string name)
        {
            name = name.ToLower();
            foreach (Servers server in servers)
            {
                if (server.Name == name)
                {
                    return (server.IP, server.Port);
                }
            }
            return (null, 0);
        }
        private async Task ProcessMessage(Messages.Message message)
        {
            if (aeskey != null)
            {
                message = Encryption.DecryptMessage(message, aeskey);
            }
            if (message.PublicKey != null)
            {
                //We have a public key from server
                aeskey = Encryption.GenerateAESKey(my, message.PublicKey);
            }
            else if (message.Auth == true)
            {
                //User is authenticated
                auth.TrySetResult(true);
                //auth = true;
            }
            else if (message.Auth == false)
            {
                auth.TrySetResult(false);
                //auth = false;
            }
            else if (message.Msg != null || message.Data != null)
            {
                await HandleMessage(message);
            }
        }
        public async Task HandleMessage(Messages.Message message)
        {
            if (main.chat != null && await ischatready.Task && message.Msg != null)
            {
                //Chat is ready
                //Check if it has file
                if (message.IsFile == true)
                {
                    await main.chat.SaveFile(message.Data);
                }
                //Display message
                main.chat.display.Invoke(() => main.chat.DisplayMessage(message));
            }
        }
        public async Task Disconnect(bool force = false)
        {
            if (!disconnectstarted)
            {
                disconnectstarted = true;
                try
                {
                    connected = false;
                    if (force || Server == null || ReconnectTimer.Interval > ReconnectTimeOut)
                    {
                        //Disconnect by clicking button
                        if (client != null)
                        {
                            client.Close();
                            client.Dispose();
                        }
                        //start new client
                        main.client = new Client(main);
                        await main.client.LoadServers();
                        //close all
                        main.CloseDisconnect(force);
                        main.ManipulateMenue(false);
                    }
                    else if (!force && ReconnectTimer.Interval < ReconnectTimeOut)
                    {
                        //Disconnect due to connection error
                        //Try reconnect if timer valid
                        ReconnectTimer.Start();
                    }
                }
                catch (Exception ex)
                {
                    //Logging
                    await WriteLog(ex);
                }
            }
        }
        private async void Reconnect(Object? source, System.Timers.ElapsedEventArgs e)
        {
            //Dispose old TClient
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
            //Reset
            disconnectstarted = false;
            if (Server != null)
            {
                await Connect(Server.Value);
                if (!connected)
                {
                    ReconnectTimer.Interval *= 2;
                }
                else
                {
                    ReconnectTimer.Stop();
                    ReconnectTimer.Interval = InitialReconnectInterval;
                    if (Username != null && Password != null)
                    {
                        //We can login back
                        await Login(Username, Password);
                    }
                    else
                    {
                        //We can't login back
                        //Disconnect and reset menue
                        await Disconnect(true);
                    }
                }
            }
        }
        public async Task LoadServers()
        {
            try
            {
                //Load servers from Servers.json
                servers.Clear();
                string jsonString = await System.IO.File.ReadAllTextAsync("Servers.json");
                var servers_list = await Task.Run(() =>
                {
                    return JsonSerializer.Deserialize<Servers[]>(jsonString);
                });
                if (servers_list != null)
                {
                    foreach (Servers server in servers_list)
                    {
                        Servers srv = server;
                        srv.Name = srv.Name.ToLower();
                        //servers.TryAdd(server.Name.ToLower(), server);
                        servers.Add(srv);
                    }
                }
            }
            catch (Exception ex)
            {
                //Logging
                await WriteLog(ex);
            }
        }
        public async Task SaveServers()
        {
            try
            {
                string jsonString = await Task.Run(() =>
                {
                    List<Servers> servers_list = [];
                    foreach (Servers server in servers)
                    {
                        /*server.Value.Name = server.Value.Name.ToLower();
                        servers_list.Add(server.Value);*/
                        servers_list.Add(server);
                    }
                    return JsonSerializer.Serialize(servers_list);
                });
                await System.IO.File.WriteAllTextAsync("Servers.json", jsonString);
            }
            catch (Exception ex)
            {
                //Logging
                await WriteLog(ex);
            }
        }
        public static async Task WriteLog(Exception ex)
        {
            string log = DateTime.Now.ToString("d.M.yyyy. H:m:s") + " " + ex.ToString() + Environment.NewLine;
            try
            {
                await System.IO.File.AppendAllTextAsync("Client.log", log);
            }
            catch (Exception)
            {
                MessageBox.Show("Can't save log to file.");
                // Console.WriteLine(log);
            }
        }
    }
}