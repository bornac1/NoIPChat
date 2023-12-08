using Messages;
using System.Collections.Concurrent;
using System.Net;
using Transport;
using System.Text;
using System.Text.Json;

namespace Client
{
    public class Client
    {
        public float CV = 1;
        public bool? auth;
        public bool connected = false;
        public ConcurrentQueue<Messages.Message> messages_snd;
        public string? Username;
        public string? Password;
        public string? Server;
        public TClient client;
        public BindingSource servers;
        public ConcurrentQueue<Messages.Message> messages_rec;
        private readonly StringBuilder value;
        public bool ischatready = false;
        public Main main;
        private bool disconnectstarted;
        public Messages.Message message;
        public Client(Main main)
        {
            this.main = main;
            disconnectstarted = false;
            messages_snd = [];
            servers = [];
            messages_rec = [];
            value = new StringBuilder();
            _ = LoadServers();
            client = new TClient(new TcpClient(new IPEndPoint(IPAddress.Any,0)));
            message = new Messages.Message();
        }
        public async Task Connect(Servers srv)
        {
            try
            {
                client = new TClient(new TcpClient(new IPEndPoint(IPAddress.Any, 0)));
                await client.ConnectAsync(IPAddress.Parse(srv.IP), srv.Port);
                connected = true;
            }
            catch (Exception ex)
            {
                if (ex is System.Net.Sockets.SocketException)
                {
                    //Error connecting
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
                    byte[]? data = await ReadData(length);
                    if (data != null)
                    {
                        Messages.Message message = await Processing.Deserialize(data);
                        await ProcessMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is System.Net.Sockets.SocketException)
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
        }
        private async Task<int> ReadLength()
        {
            byte[] buffer = new byte[sizeof(int)];
            int totalread = 0;
            int offset = 0;
            while (totalread < buffer.Length)
            {
                int read = await client.ReceiveAsync(buffer, offset, buffer.Length - totalread);
                totalread += read;
                offset += read;
            }
            return BitConverter.ToInt32(buffer, 0);
        }
        private async Task<byte[]> ReadData(int length)
        {
            byte[] buffer = new byte[length];
            int totalread = 0;
            int offset = 0;
            while (totalread < buffer.Length)
            {
                int read = await client.ReceiveAsync(buffer, offset, buffer.Length - totalread);
                totalread += read;
                offset += read;
            }
            return buffer;
        }
        public async Task Login(string username, string password)
        {
            Username = username.ToLower();
            Password = password;
            message.CV = CV;
            message.User = Username;
            message.Pass = Password;
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
                byte[]? data = await Processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(data.Length);
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
                if (ex is System.Net.Sockets.SocketException)
                {
                    //Assume disconnection
                    await Disconnect();
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
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
            }
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
            if (message.Auth == true)
            {
                //User is authenticated
                auth = true;
            }
            else if (message.Auth == false)
            {
                auth = false;
            }
            else if (message.Msg != null || message.Data != null)
            {
                await PrintMessage(message);
            }
        }
        public async Task PrintMessage(Messages.Message message)
        {
            if (main.chat != null && ischatready)
            {
                await PrintReceivedMessages();
                //Chat is ready
                string current = main.chat.display.Text;
                string newvalue = await Task.Run(() =>
                {
                    value.Append(current);
                    value.AppendLine($"{message.Sender}:{message.Msg}");
                    string str = value.ToString();
                    value.Clear();
                    return str;
                });
                main.chat.display.Text = newvalue;
            }
            else
            {
                //Chat isn't ready
                //Let's save the message
                messages_rec.Enqueue(message);
            }
        }
        public async Task PrintReceivedMessages()
        {
            if (main.chat != null && ischatready && !messages_rec.IsEmpty)
            {
                MessageBox.Show("We have " + messages_rec.Count);
                for (int i = 0; i < messages_rec.Count; i++)
                {
                    MessageBox.Show("printing " + i);
                    messages_rec.TryDequeue(out var message);
                    await PrintMessage(message);
                }
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
                catch (Exception ex)
                {
                    //Logging
                    await WriteLog(ex);
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
        public async Task WriteLog(Exception ex)
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