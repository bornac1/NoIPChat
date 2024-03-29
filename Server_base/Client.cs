﻿using MessagePack;
using Messages;
using System.Net;
using Transport;

namespace Server_base
{
    public partial class Client
    {
        private string? user = null;
        private string? name = null;
        private readonly Server server;
        private bool isserver = false; //This is connection from remote server
        private bool isremote = false; //This is connection to remote server
        private TClient client;
        private bool connected;
        private readonly System.Timers.Timer? timer;
        private bool disconnectstarted = false;
        private bool auth = false;
        private readonly string localip;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        private byte[] bufferm = new byte[1024];
        private byte[]? aeskey;
        private bool publickeysend = false;
        private readonly System.Timers.Timer? ReconnectTimer;
        private const double ReconnectTimeOut = 60000;//60 seconds
        private const double InitialReconnectInterval = 15;
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
            ReconnectTimer = new(InitialReconnectInterval);
            ReconnectTimer.Elapsed += ReconnectServer;
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
                if (ex is TransportException)
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
                    await server.WriteLog(ex);
                }
            }
        }
        private async Task ReceiveKey()
        {
            try
            {
                int length = await ReadLength();
                ReadOnlyMemory<byte>? data = null;
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
                    try
                    {
                        Message message = await Processing.Deserialize(data.Value);
                        await ProcessMessage(message);
                    } catch (MessagePackSerializationException)
                    {
                        //Message error
                        await Disconnect();
                    }
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
                if (ex is TransportException)
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
                    await server.WriteLog(ex);
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
                    ReadOnlyMemory<byte>? data = null;
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
                        try
                        {
                            Message message = await Processing.Deserialize(data.Value);
                            await ProcessMessage(message);
                        } catch (MessagePackSerializationException)
                        {
                            //Mewssage error
                            await Disconnect();
                        }
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
                    if (ex is TransportException)
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
                        await server.WriteLog(ex);
                    }
                    connected = false;
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
                if(read == 0)
                {
                    await Disconnect();
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
            int totalread = 0;
            if (length > 0)
            {
                Handlebufferm(length);
                while (totalread < length)
                {
                    int read = await client.ReceiveAsync(bufferm, totalread, length - totalread);
                    totalread += read;
                    if(read == 0)
                    {
                        await Disconnect();
                    }
                }
            }
            return new ReadOnlyMemory<byte>(bufferm, 0, totalread);
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
                        await DisconnectServer(force);
                    }
                    connected = false;
                    if (client != null)
                    {
                        client.Close(force);
                        client.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                //Should be logged
                await server.WriteLog(ex);
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
                await server.WriteLog(ex);
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
                    //Console.WriteLine("aes key is null, so we have a problem");
                    //We don't want to send unencrypted
                    await Disconnect();
                }
                byte[]? data = await Processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
                    if (connected)
                    {
                        //connected
                        //Console.WriteLine("sending to " + name + user);
                        await client.SendAsync(length);
                        //Print(length);
                        await client.SendAsync(data);
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
                        if (user != null && !await server.AddMessages(user, message))
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
                if (ex is TransportException)
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
                    await server.WriteLog(ex);
                }
                if (!msgerror)
                {
                    //Save message to be sent later
                    //Decrypt before saving
                    if (aeskey != null)
                    {
                        message = Encryption.DecryptMessage(message, aeskey);
                    }
                    if (user != null && !await server.AddMessages(user, message))
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
            if (!isserver && !isremote && user != null)
            {
                user = user.ToLower();
                if (server.messages.TryGetValue(user, out DataHandler? handler) && handler != null)
                {
                    await foreach (Message message in handler.GetMessages())
                    {
                        await SendMessage(message);
                    }
                    await handler.Delete();
                    server.messages.Remove(user, out _);
                }
            }
        }
        public async Task SendAllMessagesRemoteUser(string user)
        {
            if (isserver || isremote)
            {
                user = user.ToLower();
                if (server.messages.TryGetValue(user, out DataHandler? handler) && handler != null)
                {
                    await foreach (Message message in handler.GetMessages())
                    {
                        await SendMessage(message);
                    }
                    await handler.Delete();
                    server.messages.TryRemove(user, out _);
                }
            }
        }
        public async Task SendAllMessagesServer()
        {
            if ((isserver || isremote) && name != null)
            {
                name = name.ToLower();
                if (server.messages_server.TryGetValue(name, out DataHandler? handler) && handler != null)
                {
                    await foreach (Message message in handler.GetMessages())
                    {
                        await SendMessage(message);
                    }
                    await handler.Delete();
                    server.messages_server.TryRemove(name, out _);
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
                        if (MemoryExtensions.Equals(StringProcessing.GetServer(user), name, StringComparison.OrdinalIgnoreCase))
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
                await server.WriteLog(ex);
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
