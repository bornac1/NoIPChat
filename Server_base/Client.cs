using MessagePack;
using Messages;
using System.Net;
using Transport;

namespace Server
{
    public partial class Client
    {
        private string user = "";
        private string name = "";
        private readonly Server server;
        private bool isserver = false; //This is connection from remote server
        private bool isremote = false; //This is connection to remote server
        private readonly TClient client;
        private bool connected;
        private readonly System.Timers.Timer? timer;
        private bool disconnectstarted;
        private bool auth = false;
        private readonly string localip;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        public Client(Server server, TClient client, string localip)
        {
            this.server = server;
            this.client = client;
            this.localip = localip;
            disconnectstarted = false;
            connected = true;
            _ = Receive();
        }
        public Client(Server server, string name, string localip, string ip, int port, int timeout)
        {
            this.server = server;
            isremote = true;
            this.name = name;
            client = new TClient(new TcpClient(IPEndPoint.Parse(localip)));
            this.localip = localip;
            disconnectstarted = false;
            if (timeout != 0)
            {
                timer = new System.Timers.Timer
                {
                    Interval = timeout * 1000
                };
                timer.Elapsed += TimeoutHanlder;
                timer.Start();
            }
            _ = Connect(ip, port);
        }
        private async Task Connect(string ip, int port)
        {
            try
            {
                await client.ConnectAsync(IPAddress.Parse(ip), port);
                connected = true;
                isremote = true;
                //Send welcome
                var data = await server.GetInterfacebyIP(localip);
                if (data.Item1 != "")
                {
                    Message message = new()
                    {
                        Name = server.name.ToLower(),
                        Server = true,
                        SV = server.SV,
                        Data = await Task.Run(() => { return MessagePackSerializer.Serialize(new ServerData() { IP = data.Item1, Port = data.Item2 }); })
                    };
                    await SendMessage(message);
                    _ = Receive();
                }
                else
                {
                    throw new Exception("GetInterfacebyIP error");
                }
            }
            catch (Exception ex)
            {
                //Exception should be logged
                if (ex is System.Net.Sockets.SocketException)
                {
                    //Problem connecting
                    //No need for logging
                    await Disconnect();
                }
                else
                {
                    //Other problem
                    //Should be logged
                    await Disconnect();
                    await Server.WriteLog(ex);
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
                    byte[]? data = null;
                    if (length < 1024 || auth)
                    {
                        //Non authenticated is limited to 1024
                        data = await ReadData(length);
                    }
                    if (data != null)
                    {
                        Message message = await Processing.Deserialize(data);
                        await ProcessMessage(message);
                        if (timer != null)
                        {
                            //Reset timer
                            timer.Stop();
                            timer.Start();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is System.Net.Sockets.SocketException)
                    {
                        //assume disconnection
                        //No need for logging
                    }
                    else if (ex is ObjectDisposedException)
                    {
                        //already disposed
                        //No need for logging
                    }
                    else
                    {
                        //Should be logged
                        await Server.WriteLog(ex);
                    }
                    connected = false;
                    await Disconnect();
                }
            }
        }
        private async Task<int> ReadLength()
        {
            int totalread = 0;
            int offset = 0;
            while (totalread < bufferl.Length)
            {
                int read = await client.ReceiveAsync(bufferl, offset, bufferl.Length - totalread);
                totalread += read;
                offset += read;
            }
            return BitConverter.ToInt32(bufferl, 0);
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
        public async Task Disconnect(bool force = false)
        {
            try
            {
                if (!disconnectstarted)
                {
                    disconnectstarted = true;
                    if (!isserver)
                    {
                        //Disconnect clients
                        await DisconnectClient(force);
                    }
                    else if (isserver || isremote)
                    {
                        DisconnectServer();
                    }
                    connected = false;
                    if (client != null)
                    {
                        client.Close();
                        client.Dispose();
                    }
                    if (isserver || isremote)
                    {
                        //Try to reconnect to remote server
                    }
                }
            }
            catch (Exception ex)
            {
                //Should be logged
                await Server.WriteLog(ex);
            }
        }
        private void DisconnectServer()
        {
            if (!server.remoteservers.TryRemove(name.ToLower(), out _))
            {
                //Remote server is already removed
            }
            foreach (string user in server.remoteusers.Keys)
            {
                if (server.remoteusers.TryGetValue(user.ToLower(), out string? srv))
                {
                    if (srv == name)
                    {
                        if (!server.remoteusers.TryRemove(user.ToLower(), out _))
                        {
                            //Is it already removed
                        }
                    }
                }
            }
        }
        private async Task DisconnectClient(bool force)
        {
            if (force && connected)
            {
                Message message1 = new()
                {
                    Disconnect = true
                };
                await SendMessage(message1);
            }
            if (!server.clients.TryRemove(user.ToLower(), out _))
            {
                //Probably already removed or not added at all
            }
            var usr = user.Split("@");
            if (usr[1] != server.name)
            {
                //User home server is remote
            }
        }
        private void DisconnectRemoteUser(Message message)
        {
            if (isserver || isremote)
            {
                if (message.User != null)
                {
                    if (!server.remoteusers.TryRemove(message.User.ToLower(), out _))
                    {
                        //User wasn't even in dictionary
                    }
                }
            }
        }
        private async Task ProcessMessage(Message message)
        {
            try
            {
                if (message.Server == true)
                {
                    isserver = true;
                    //Process from server's welcome message
                }
                if (isserver || isremote)
                {
                    //Server is connected
                }
                else if (!isserver && !isremote)
                {
                    //Client is connected
                    await ProcessClientMessage(message);
                }
            }
            catch (Exception ex)
            {
                //Should be logged
                await Server.WriteLog(ex);
            }
        }
        public async Task<bool> SendMessage(Message message)
        {
            bool msgerror = false;
            try
            {
                byte[]? data = await Processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(data.Length);
                    if (connected)
                    {
                        //connected
                        await client.SendAsync(length);
                        await client.SendAsync(data);
                        //Reset timer
                        if (timer != null)
                        {
                            timer.Stop();
                            timer.Start();
                        }
                        return true;
                    }
                    else
                    {
                        //Not connected
                        //Save message
                        if (!server.AddMessages(user, message))
                        {
                            return false;
                        }
                        return true;
                    }
                }
                else
                {
                    msgerror = true;
                    return false;
                    //Console.WriteLine("Message error");
                }
            }
            catch (Exception ex)
            {
                if (ex is System.Net.Sockets.SocketException)
                {
                    //assume disconnection
                    //No need for logging
                }
                else if (ex is ObjectDisposedException)
                {
                    //already disposed
                    //No need for logging
                }
                else
                {
                    //Should be logged
                    await Server.WriteLog(ex);
                }
                if (!msgerror)
                {
                    //Save message to be sent later
                    if (!server.AddMessages(user, message))
                    {
                        //Don't know why
                    }
                }
                connected = false;
                await Disconnect();
            }
            return false;
        }
        public async Task SendAllMessages()
        {
            if (!isserver && !isremote)
            {
                if (server.messages.TryGetValue(user.ToLower(), out var messages))
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        messages.TryDequeue(out Message message);
                        await SendMessage(message);
                    }
                    if (messages.IsEmpty)
                    {
                        if (!server.messages.TryRemove(user.ToLower(), out _))
                        {
                            //Doesn't exsist anymore
                        }
                    }
                }
            }
        }
        public async Task SendAllMessagesRemoteUser(string user)
        {
            if (isserver || isremote)
            {
                if (server.messages.TryGetValue(user.ToLower(), out var messages))
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        messages.TryDequeue(out Message message);
                        await SendMessage(message);
                    }
                    if (messages.IsEmpty)
                    {
                        if (!server.messages.TryRemove(user.ToLower(), out _))
                        {
                            //Doesn't exsists anymore
                        }
                    }
                }
            }
        }
        public async Task SendAllMessagesServer()
        {
            if (isserver || isremote)
            {
                if (server.messages_server.TryGetValue(name.ToLower(), out var messages))
                {
                    for (int i = 0; i < messages.Count; i++)
                    {
                        messages.TryDequeue(out Message message);
                        await SendMessage(message);
                    }
                    if (messages.IsEmpty)
                    {
                        if (!server.messages.TryRemove(user.ToLower(), out _))
                        {
                            //Doesn't exsisst anymore
                        }
                    }
                }
            }
        }
        private async Task DisconnectNoUse()
        {
            try
            {
                //Disconnect server for which there is no longer need
                //Called after timeout
                if (isserver || isremote)
                {
                    bool disc = true;
                    foreach (string user in server.remoteusers.Keys)
                    {
                        if (server.remoteusers.TryGetValue(user.ToLower(), out string? srv))
                        {
                            if (srv == name)
                            {
                                //This is user's home server
                                //But user is connected to remote one
                                disc = false;
                                break;
                            }
                        }
                    }
                    foreach (string user in server.clients.Keys)
                    {
                        if (user.Split("@")[1].Equals(name, StringComparison.CurrentCultureIgnoreCase))
                        {
                            //User is connected to this server
                            //But user's home server is remote
                            disc = false;
                            break;
                        }
                    }
                    if (disc)
                    {
                        Message message = new()
                        {
                            SV = server.SV,
                            Disconnect = true
                        };
                        await SendMessage(message);
                        await Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                //Logging just in case
                await Server.WriteLog(ex);
            }
        }
        private void TimeoutHanlder(Object? source, System.Timers.ElapsedEventArgs e)
        {
            _ = DisconnectNoUse();
        }
       private static void Print(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                string byteString = b.ToString("X2"); // Convert to hexadecimal string
                Console.Write(byteString + " ");
            }
        }
    }
}
