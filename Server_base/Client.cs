using Messages;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class Client
    {
        private string user = "";
        private string name = "";
        private readonly Server server;
        private bool isserver = false; //This is connection from remote server
        private bool isremote = false; //This is connection to remote server
        private readonly TcpClient client;
        private NetworkStream? stream;
        private readonly byte[] buffer = new byte[1024];
        private bool connected;
        private readonly Processing processing;
        private int bytesRead;
        private int bufferOffset;
        private readonly System.Timers.Timer? timer;
        private bool disconnectstarted;
        public Client(Server server, TcpClient client)
        {
            this.server = server;
            this.client = client;
            stream = client.GetStream();
            disconnectstarted = false;
            connected = true;
            processing = new Processing();
            _ = Receive();
            Console.WriteLine("Accepted.");
        }
        public Client(Server server, string name, string localip, string ip, int port, int timeout)
        {
            this.server = server;
            isremote = true;
            client = new TcpClient(IPEndPoint.Parse(localip));
            processing = new Processing();
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
            _ = Connect(name, ip, port);
        }
        private async Task Connect(string name, string ip, int port)
        {
            //to do: try, catch
            await client.ConnectAsync(IPAddress.Parse(ip), port);
            stream = client.GetStream();
            connected = true;
            isremote = true;
            //Send welcome
            Message message = new()
            {
                Name = server.name.ToLower(),
                Server = true,
                SV = server.SV,
                Users = server.GetUsersServer(name)
            };
            await SendMessage(message);
            _ = Receive();
        }
        private async Task Receive()
        {
            while (connected)
            {
                try
                {
                    int availableBytes = bytesRead - bufferOffset;

                    // Check if we have enough bytes in the buffer to read the size
                    if (availableBytes >= sizeof(int))
                    {
                        int messageSize = BitConverter.ToInt32(buffer, bufferOffset);
                        int totalMessageSize = sizeof(int) + messageSize;

                        // Check if the entire message fits in the buffer
                        if (totalMessageSize <= availableBytes)
                        {
                            byte[] messageBytes = new byte[messageSize];
                            Array.Copy(buffer, bufferOffset + sizeof(int), messageBytes, 0, messageSize);

                            // Move the remaining bytes in the buffer to the beginning
                            Array.Copy(buffer, bufferOffset + totalMessageSize, buffer, 0, availableBytes - totalMessageSize);

                            // Update the bytesRead and bufferOffset variables
                            bytesRead = availableBytes - totalMessageSize;
                            bufferOffset = 0;

                            //Message processing starts
                            try
                            {
                                Message message = await processing.Deserialize(messageBytes);
                                await ProcessMessage(message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Message deseialization error");
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }

                    // Read more bytes from the stream
                    if (stream != null)
                    {
                        int bytesReadNow = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);

                        // Check if the stream has reached its end
                        if (bytesReadNow == 0)
                        {
                            connected = false;
                            break;
                        }

                        bytesRead += bytesReadNow;
                        if (timer != null)
                        {
                            //Reset timer
                            timer.Stop();
                            timer.Start();
                        }
                    }
                }
                catch (Exception ex)
                { //assume disconnection
                    connected = false;
                    await Disconnect();
                }
            }
        }
        private async Task Login(Message message)
        {
            if (!isserver && !isremote)
            {
                //Client is connected
                if (message.User != null)
                {
                    user = message.User.ToLower();
                    if (!server.clients.TryAdd(user, this))
                    {
                        //Already exsists
                        if (server.clients.TryGetValue(user, out var cli) && cli != null)
                        {
                            await cli.Disconnect();
                            if (!server.clients.TryAdd(user, this))
                            {
                                //Fails once again
                                //Don't know why
                                Console.WriteLine("Error add to clients list");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Error add to clients list");
                        }
                    }
                    var usr = user.Split("@");

                    if (usr[1] == server.name)
                    {
                        //User home is current server
                        Message msg = new()
                        {
                            Auth = true
                        };
                        Console.WriteLine("Autheticated");
                        await SendMessage(msg);
                        await SendAllMessages();
                    }
                    else
                    {
                        //User home is other server
                        message.Sender = server.name;
                        message.Receiver = usr[1];
                        await server.SendMessageRemote(usr[1], message);
                    }
                }
            }
            else
            {
                //We got a login from remote server
                if (message.User != null)
                {
                    var usr = message.User.Split("@");
                    if (usr[1].Equals(server.name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        //User home is this server
                        Message msg = new()
                        {
                            Auth = true,
                            Sender = server.name,
                            Receiver = message.User
                        };
                        if (!server.remoteusers.TryAdd(message.User.ToLower(), server.name))
                        {
                            //Already exsists
                            Console.WriteLine("Error add to remote users list");
                        }
                        await SendMessage(msg);
                        await SendAllMessagesRemoteUser(message.User);
                    }
                    else
                    {
                        //User home is other server
                        //Used for multi-hop relay
                        /*message.Sender = server.name;
                        message.Receiver = usr[1];
                        await server.SendMessageRemote(usr[1], message);*/
                    }
                }
            }
        }
        public async Task Disconnect(bool force = false)
        {
            if (!disconnectstarted)
            {
                disconnectstarted = true;
                if (!isserver)
                {
                    if (force && connected)
                    {
                        Message message1 = new()
                        {
                            Disconnect = true
                        };
                        await SendMessage(message1);
                    }
                    Console.WriteLine(server.clients.Count);
                    if (!server.clients.TryRemove(user.ToLower(), out _))
                    {
                        Console.WriteLine("Error remove client from list");
                    }
                    var usr = user.Split("@");
                    if (usr[1] != server.name)
                    {
                        //User home server is remote
                        Message message = new()
                        {
                            Disconnect = true,
                            User = user,
                            Sender = server.name,
                            Receiver = usr[1]
                        };
                        await server.SendMessageRemote(usr[1], message);
                    }
                }
                else if (isserver || isremote)
                {
                    if (!server.remoteservers.TryRemove(name.ToLower(), out _))
                    {
                        Console.WriteLine("Error remove remote server from list");
                    }
                    foreach (string user in server.remoteusers.Keys)
                    {
                        if (server.remoteusers.TryGetValue(user.ToLower(), out string? srv))
                        {
                            if (srv == name)
                            {
                                if (!server.remoteusers.TryRemove(user.ToLower(), out _))
                                {
                                    Console.WriteLine("Error remove remote user from list by server");
                                }
                            }
                        }
                    }
                }
                connected = false;
                if (stream != null)
                {
                    await stream.FlushAsync();
                    stream.Close();
                    await stream.DisposeAsync();
                }
                client.Close();
                client.Dispose();
                await processing.Close();
                if (isserver || isremote)
                {
                    //Try to reconnect to remote server
                }
            }
        }
        private void DisconnectRemoteUser(Message message)
        {
            if (isserver || isremote)
                {
                    Console.WriteLine("Deleting " + message.User);
                    if (message.User != null)
                    {
                        if (!server.remoteusers.TryRemove(message.User.ToLower(), out _))
                        {
                            Console.WriteLine("Error remove remote user from list");
                        }
                        Console.WriteLine("Done disconnect remote" + server.remoteusers.Count);
                    }
                }
        }
        private async Task ProcessMessage(Message message)
        {
            if (message.Server == true)
            {
                isserver = true;
                //Process from server's welcome message
                if (message.Name != null)
                {
                    name = message.Name;
                    if (message.Users != null)
                    {
                        foreach (string usr in message.Users.Split(";"))
                        {
                            if (usr != null && usr != "")
                            {
                                if (!server.remoteusers.TryAdd(usr.ToLower(), name))
                                {
                                    Console.WriteLine("Error add to remote users list");
                                }
                            }
                        }
                    }
                    //to implement: version checking
                    if (!server.remoteservers.TryAdd(name.ToLower(), this))
                    {
                        Console.WriteLine("Error add to remote servers list");
                    }
                    if (!isremote)
                    {
                        //Send welcome message to remote server
                        Message msg = new()
                        {
                            Name = server.name,
                            Server = true,
                            SV = server.SV
                        };
                        await SendMessage(msg);
                    }
                    await SendAllMessagesServer();
                }
            }
            else if (isserver)
            {
                //Server is connected
                if (message.Disconnect == true && message.User != null)
                {
                    //Dsiconnect user on remote server
                    DisconnectRemoteUser(message);
                }
                else if (message.Disconnect == true && message.User == null)
                {
                    //Disconnect remote server
                    await Disconnect();
                }
                else if (message.User != null && message.Pass != null)
                {
                    //Login message received
                    await Login(message);
                }
                else if (message.Auth != null)
                {
                    //Authentication message
                    if (message.Receiver != null)
                    {
                        await server.SendMessage(message.Receiver, message);
                    }
                }
                else if (message.Msg != null || message.Data != null)
                {
                    //Normal message
                    if (message.Receiver != null)
                    {
                        await server.SendMessage(message.Receiver, message);
                    }
                }
            }
            if (!isserver)
            {
                //Client is connected
                if (message.User != null && message.Pass != null)
                {
                    await Login(message);
                }
                else if (message.Disconnect == true)
                {
                    await Disconnect();
                }
                else if (message.Msg != null || message.Data != null)
                {
                    //Will send whole message to recivers (Msg+Data)
                    message.Auth = null;
                    message.User = null;
                    message.Pass = null;
                    message.Disconnect = null;
                    if (message.Receiver != null)
                    {
                        string[] recivers = message.Receiver.Split(';');
                        //Split messages for each receiver
                        foreach (string reciver in recivers)
                        {
                            message.Receiver = reciver;
                            string[] rcv = reciver.Split("@");
                            if (rcv[1].Equals(server.name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                //Receiver is on the same server
                                await server.SendMessage(reciver, message);
                            }
                            else
                            {
                                //Receiver is on the other server
                                await server.SendMessageRemote(rcv[1], message);
                            }
                        }
                    }
                }
            }
        }
        public async Task<bool> SendMessage(Message message)
        {
            if (connected)
            {
                bool msgerror = false;
                try
                {
                    byte[] data = await processing.Serialize(message);
                    if (data != null)
                    {
                        byte[] length = BitConverter.GetBytes(data.Length);
                        if (stream != null)
                        {
                            await stream.WriteAsync(length);
                            await stream.WriteAsync(data);
                            //Reset timer
                            if (timer != null)
                            {
                                timer.Stop();
                                timer.Start();
                            }
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        msgerror = true;
                        Console.WriteLine("Message error");
                    }
                }
                catch (Exception ex)
                {
                    //Assume disconnection
                    connected = false;
                    await Disconnect();
                    if (!msgerror)
                    {
                        //Save message to be sent later
                        if (!server.AddMessages(user, message))
                        {
                            Console.WriteLine("Error adding to messages list.");
                        }
                    }
                }
            }
            return false;
        }
        public async Task SendAllMessages()
        {
            if (!isserver)
            {
                if (server.messages.TryGetValue(user.ToLower(), out var messages))
                {
                    foreach (Message message in messages)
                    {
                        await SendMessage(message);
                    }
                    Console.WriteLine("Done send all");
                    if (messages.IsEmpty)
                    {
                        if (!server.messages.TryRemove(user.ToLower(), out _))
                        {
                            Console.WriteLine("All sent, error removing");
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
                    foreach (Message message in messages)
                    {
                        await SendMessage(message);
                        //Debug start
                        Console.WriteLine("sending");
#pragma warning disable CS8604 // Possible null reference argument.
                        Console.WriteLine(message.Sender, message.Receiver, message.Msg);
#pragma warning restore CS8604 // Possible null reference argument.
                        //Debug end
                    }
                    if (messages.IsEmpty)
                    {
                        if (!server.messages.TryRemove(user.ToLower(), out _))
                        {
                            Console.WriteLine("All sent, error removing");
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
                    foreach (Message message in messages)
                    {
                        await SendMessage(message);
                    }
                    if (messages.IsEmpty)
                    {
                        if (!server.messages.TryRemove(user.ToLower(), out _))
                        {
                            Console.WriteLine("All sent, error removing by server");
                        }
                    }
                }
            }
        }
        private async Task DisconnectNoUse()
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
