using Messages;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public partial class Client
    {
        private string user = "";
        private string name = "";
        private readonly Server server;
        private bool isserver = false; //This is connection from remote server
        private bool isremote = false; //This is connection to remote server
        private readonly TcpClient client;
        private NetworkStream? stream;
        private byte[] buffer = new byte[1024];
        private bool connected;
        private readonly Processing processing;
        private readonly System.Timers.Timer? timer;
        private bool disconnectstarted;
        private bool auth = false;
        public Client(Server server, TcpClient client)
        {
            this.server = server;
            this.client = client;
            stream = client.GetStream();
            disconnectstarted = false;
            connected = true;
            processing = new Processing();
            _ = Receive();
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
            try
            {
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
            } catch (Exception ex) { 
                //Exception should be logged
                if(ex is SocketException)
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
        private async Task Receive()
        {
            int bytesRead = 0;
            int bufferOffset = 0;
            using (MemoryStream memoryStream = new MemoryStream())
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

                            if (messageSize <= buffer.Length)
                            {
                                //Message can fit into buffer

                                if(messageSize*2 < buffer.Length && buffer.Length > 1024)
                                {
                                    //Buffer is too large

                                    //Create new buffer
                                    byte[] newbuffer = new byte[totalMessageSize];
                                    Console.WriteLine("new buffer created " + totalMessageSize);

                                    //Copy to new buffer
                                    Array.Copy(buffer, bufferOffset, newbuffer, 0, availableBytes);

                                    //Update
                                    buffer = newbuffer;
                                    bufferOffset = 0;
                                }

                                // Check if the entire message is already in the buffer
                                if (totalMessageSize <= availableBytes)
                                {
                                    //Write message to memorystream
                                    await memoryStream.WriteAsync(buffer, bufferOffset + sizeof(int), messageSize);
                                    await memoryStream.FlushAsync();

                                    // Move the remaining bytes in the buffer to the beginning
                                    Array.Copy(buffer, bufferOffset + totalMessageSize, buffer, 0, availableBytes - totalMessageSize);

                                    // Update the bytesRead and bufferOffset variables
                                    bytesRead = availableBytes - totalMessageSize;
                                    bufferOffset = 0;

                                    //Message processing starts
                                    Message message = await processing.Deserialize(memoryStream);
                                    await ProcessMessage(message);
                                }
                            }
                            else
                            {
                                //Message can't fit into buffer
                                if (auth)
                                {
                                    //Do it only if client is authenticated
                                    //Create new buffer
                                    byte[] newbuffer = new byte[totalMessageSize];
                                    Console.WriteLine("new buffer created " + totalMessageSize);

                                    //Copy to new buffer
                                    Array.Copy(buffer, bufferOffset, newbuffer, 0, availableBytes);

                                    //Update
                                    buffer = newbuffer;
                                    bufferOffset = 0;
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
                    {
                        if (ex is IOException)
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
        }
        private async Task Login(Message message)
        {
            try
            {
                if (!isserver && !isremote)
                {
                    //Client is connected
                    await LoginClient(message);
                }
                else
                {
                    //We got a login from remote server
                    await LoginRemoteServer(message);
                }
                auth = true;
            } catch (Exception ex) {
                //Should be logged
                await server.WriteLog(ex);
            }
        }
        private async Task LoginRemoteServer(Message message)
        {
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
        private async Task LoginClient(Message message)
        {
            if (message.User != null)
            {
                user = message.User.ToLower();
                if (!server.clients.TryAdd(user, this))
                {
                    if (server.clients.TryGetValue(user, out var cli) && cli != null)
                    {
                        //Already exsists
                        await cli.Disconnect();
                        if (!server.clients.TryAdd(user, this))
                        {
                            //Fails once again
                            //Don't know why
                        }
                    }
                    else
                    {
                        //Doesn't exsist already
                        //Why it fails??????
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
                    if (stream != null)
                    {
                        await stream.FlushAsync();
                        stream.Close();
                        await stream.DisposeAsync();
                    }
                    if (client != null)
                    {
                        client.Close();
                        client.Dispose();
                    }
                    await processing.Close();
                    if (isserver || isremote)
                    {
                        //Try to reconnect to remote server
                    }
                }
            } catch (Exception ex) {
                //Should be logged
                await server.WriteLog(ex);
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
                    await ProcessRemoteServerWelcomeMessage(message);
                }
                if (isserver)
                {
                    //Server is connected
                    await ProcessRemoteServerMessage(message);
                }
                else if (!isserver)
                {
                    //Client is connected or this is connection to remote server
                    await ProcessClientMessage(message);
                }
            } catch (Exception ex)
            {
                //Should be logged
                await server.WriteLog(ex);
            }
        }
        private async Task ProcessRemoteServerMessage(Message message)
        {
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
        private async Task ProcessRemoteServerWelcomeMessage(Message message)
        {
            if (message.Name != null)
            {
                name = message.Name.ToLower();
                if (!server.GetServer(name).Item1)
                {
                    //Unknown server

                    //Save it

                    //Save to file
                }
                if (message.Users != null)
                {
                    foreach (string usr in message.Users.Split(";"))
                    {
                        if (usr != null && usr != "")
                        {
                            string usrl = usr.ToLower();
                            if (!server.remoteusers.TryAdd(usrl, name))
                            {
                                //Already exsists in dictionary
                                if (server.remoteusers.TryGetValue(usrl, out string? srv_name))
                                {
                                    //Try to get it
                                    if (srv_name != null && srv_name != name)
                                    {
                                        //Change to current server name if it isn't
                                        server.remoteusers.TryUpdate(usrl, name, srv_name);
                                    }
                                }
                            }
                        }
                    }
                }
                //to implement: version checking
                if (!server.remoteservers.TryAdd(name.ToLower(), this))
                {
                    //Already exsists
                    //Disconnect previous one
                    if (server.remoteservers.TryGetValue(name.ToLower(), out Client? cli))
                    {
                        if (cli != null)
                        {
                            //Disconnect also deleted current one
                            await cli.Disconnect();
                            //Try once again
                            if (!server.remoteservers.TryAdd(name.ToLower(), this))
                            {
                                //Don't know why
                            }
                        }
                    }
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
        private async Task ProcessClientMessage(Message message)
        {
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
        public async Task<bool> SendMessage(Message message)
        {
            bool msgerror = false;
            try
            {
                byte[]? data = await processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(data.Length);
                    if (connected && stream != null)
                    {
                        //connected
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
                    else
                    {
                        //Not connected
                        //Save message
                        if (!server.AddMessages(user, message))
                        {
                            return false;
                        }
                        Console.WriteLine("saved " + message.Msg + " for " + message.Receiver);
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
                //Assume disconnection
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
            if (!isserver)
            {
                if (server.messages.TryGetValue(user.ToLower(), out var messages))
                {
                    Console.WriteLine("have to send " + messages.Count);
                    for (int i =0; i< messages.Count; i++)
                    {
                        Console.WriteLine("sending " + i);
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
                    for (int i = 0; i< messages.Count; i++)
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
            } catch (Exception ex)
            {
                //Logging just in case
                await server.WriteLog(ex);
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
