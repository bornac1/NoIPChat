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
        private bool disconnectstarted = false;
        private bool auth = false;
        private readonly string localip;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        private byte[]? aeskey;
        private bool publickeysend = false;
        public Client(Server server, TClient client, string localip)
        {
            this.server = server;
            this.client = client;
            this.localip = localip;
            connected = true;
            _ = Receive();
        }
        private Client(Server server, string name, string localip, int timeout)
        {
            this.server = server;
            isremote = true;
            this.name = name;
            client = new TClient(new TcpClient(IPEndPoint.Parse(localip)));
            this.localip = localip;
            if (timeout != 0)
            {
                timer = new System.Timers.Timer
                {
                    Interval = timeout * 1000
                };
                timer.Elapsed += TimeoutHanlder;
                timer.Start();
            }
        }
        public static async Task<Client> CreateAsync(Server server, string name, string localip, string ip, int port, int timeout)
        {
            Client client = new(server, name, localip, timeout);
            await client.Connect(ip, port);
            return client;
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
                await SendMessage(new Message()
                {
                    PublicKey = server.my.PublicKey
                }, false);
                publickeysend = true;
                await ReceiveKey();
                if (data.Item1 != "")
                {
                    await SendMessage(new Message()
                    {
                        Name = server.name.ToLower(),
                        Server = true,
                        SV = server.SV,
                        Data = await Task.Run(() => { return MessagePackSerializer.Serialize(new ServerData() { IP = data.Item1, Port = data.Item2 }); })
                    });
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
        private async Task ReceiveKey()
        {
            try
            {
                int length = await ReadLength();
                byte[]? data = null;
                if (length < 1024)
                {
                    //Non authenticated is limited to 1024
                    data = await ReadData(length);
                }
                if (data != null)
                {
                    //Console.WriteLine("received from " + name + user);
                    //Print(data);
                    //Console.WriteLine("end receive");
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
                    //Console.WriteLine(ex.ToString());
                    await Server.WriteLog(ex);
                }
                connected = false;
                await Disconnect();
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
                        //Console.WriteLine("received from " + name + user);
                        //Print(data);
                        //Console.WriteLine("end receive");
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
                        Console.WriteLine(ex.ToString());
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
                    if (!isserver && !isremote)
                    {
                        //Disconnect client
                        await DisconnectClient(force);
                    }
                    else if (isserver || isremote)
                    {
                        //Disconnect server
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
        private async Task ProcessMessage(Message message)
        {
            try
            {
                if (message.PublicKey != null)
                {
                    //We get a public key message
                    if (!publickeysend)
                    {
                        //We haven't already sent our public key
                        await SendMessage(new Message()
                        {
                            PublicKey = server.my.PublicKey
                        }, false);
                        publickeysend = true;
                    }
                    //Generate aeskey
                    aeskey = Encryption.GenerateAESKey(server.my, message.PublicKey);
                }
                if (message.Server == true)
                {
                    isserver = true;
                    //Process from server's welcome message
                    await ProcessServerWelcomeMessage(message);
                }
                if (isserver || isremote)
                {
                    //Server is connected
                    await ProcessServerMessage(message);
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
        public async Task<bool> SendMessage(Message message, bool encrypt = true)
        {
            bool msgerror = false;
            try
            {
                if (encrypt && aeskey != null)
                {
                    /*while (aeskey == null)
                    {
                        await Task.Delay(1);
                    }*/
                    message = Encryption.EncryptMessage(message, aeskey);
                }
                else if (aeskey == null && encrypt)
                {
                    Console.WriteLine("aes key is null, so we have a problem");
                }
                byte[]? data = await Processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(data.Length);
                    if (connected)
                    {
                        //connected
                        await client.SendAsync(length);
                        await client.SendAsync(data);
                        //Console.WriteLine("sending to " + name + user);
                        //Print(data);
                        //Console.WriteLine("end send");
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
                        //Decrypt before saving
                        if (aeskey != null)
                        {
                            message = Encryption.DecryptMessage(message, aeskey);
                        }
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
                    //Decrypt before saving
                    if (aeskey != null)
                    {
                        message = Encryption.EncryptMessage(message, aeskey);
                    }
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
                    while (messages.TryDequeue(out Message message))
                    {
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
                    while (messages.TryDequeue(out Message message))
                    {
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
                    while (messages.TryDequeue(out Message message))
                    {
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
       /*private static void Print(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                string byteString = b.ToString("X2"); // Convert to hexadecimal string
                Console.Write(byteString + " ");
            }
        }*/
    }
}
