using MessagePack;
using Messages;

namespace Server
{
    public partial class Client
    {
        //Handles connected servers
        /// <summary>
        /// Processes messages received from connected servers.
        /// </summary>
        /// <param name="message">Message to be processed.</param>
        /// <returns>Async Task.</returns>
        private async Task ProcessServerMessage(Message message)
        {
            if (message.Users != null)
            {
                //Users message
                await ProcessUsers(message.Users);
            }
            else if (message.Sender != null && message.Receiver != null)
            {
                //Mesage to be relayed
                await ProcessServerRelayMessage(message);
            }
            else if (message.User != null && message.Pass != null)
            {
                //Login message
                await ProcessServerLoginMessage(message);
            }
            else if (message.User != null && message.Auth != null)
            {
                //Auth message
                await ProcessServerAuthMessage(message);
            }
            else if (message.Receiver != null && message.Receiver == server.name)
            {
                //Message for this server
                await ProcessServerLocalMessage(message);
            }
        }
        private async Task ProcessServerWelcomeMessage(Message message)
        {
            if (message.Name != null && message.Data != null)
            {
                string name = message.Name.ToLower();
                //Add to servers
                if (!server.remoteservers.TryAdd(name, this))
                {
                    //Already exsists
                    if (server.remoteservers.TryGetValue(name, out Client? cli) && cli != null)
                    {
                        //Disconnect it
                        await cli.Disconnect();
                        if (!server.remoteservers.TryAdd(name, this))
                        {
                            //Don't know why
                        }
                    }
                }
                ServerData data = new();
                try
                {
                    data = await Task.Run(() => { return MessagePackSerializer.Deserialize<ServerData>(message.Data); });
                }
                catch
                {
                    //Just so it doesn't crash
                }
                if (data.IP != null && data.Port != null)
                {
                    if (server.servers.TryGetValue(name, out Servers? srv) && srv != null)
                    {
                        if (srv.LocalIP != localip || srv.RemoteIP != data.IP || srv.RemotePort != data.Port)
                        {
                            //Server data has changed

                            //Update values
                            srv.LocalIP = localip;
                            srv.RemoteIP = data.IP;
                            srv.RemotePort = (int)data.Port;
                        }
                    }
                    else
                    {
                        //Server is unknow
                        //Try to add it
                        if (!server.servers.TryAdd(name, new Servers() { Name = name, LocalIP = localip, RemoteIP = data.IP, RemotePort = (int)data.Port, TimeOut = 0 }))
                        {
                            //Don't know why it would fail
                        }
                    }
                }
                auth = true;
                await SendUsers();
                await SendAllMessagesServer();
            }
        }
        /// <summary>
        /// Sends data about users connected to this server, but their home server is "name"
        /// </summary>
        /// <returns>Async Task.</returns>
        private async Task SendUsers()
        {
            string? users = server.GetUsersServer(name);
            if (users != null)
            {
                await SendMessage(new Message()
                {
                    SV = server.SV,
                    Name = server.name,
                    Users = users
                });
            }
        }
        private async Task ProcessUsers(string users)
        {
            await Task.Run(() =>
            {
                string[] usersl = users.Split(';');
                foreach (string user in usersl)
                {
                    if (!server.remoteusers.TryAdd(name, user))
                    {
                        //Already exists
                    }
                }
            });
        }
        private async Task ProcessServerRelayMessage(Message message)
        {
            Console.WriteLine("Relay " + message.Msg);
            if (message.Receiver != null)
            {
                string[] rec = message.Receiver.Split('@');
                if (rec[1] == server.name)
                {
                    //This is message for user who's home server is this one
                    await server.SendMessageThisServer(message.Receiver, message);
                }
                else
                {
                    //This is message for user who's home server is other one
                    await server.SendMessageOtherServer(rec[1], message);
                }
            }
        }
        private async Task ProcessServerLoginMessage(Message message)
        {
            if (message.User != null && message.Pass != null)
            {
                string[] usr = message.User.Split("@");
                if (usr[1] == server.name)
                {
                    //Users home server is this one

                    //Authenticate
                    if (message.Pass != string.Empty)
                    {
                        await SendMessage(new Message()
                        {
                            Sender = server.name,
                            Receiver = message.User,
                            Auth = true
                        });
                    }
                    await SendAllMessagesRemoteUser(message.User);
                }
                else
                {
                    //Users home server is other one
                    //Multi hop
                }
            }
        }
        private async Task ProcessServerAuthMessage(Message message)
        {
            if (message.User != null)
            {
                string[] usr = user.Split("@");
                if (usr[1] != server.name)
                {
                    //Just to make sure
                    if (server.clients.TryGetValue(message.User, out Client? cli))
                    {
                        //User is connected to this server
                        await cli.SendMessage(message);
                    }
                    else
                    {
                        //User is not connected to this server
                        //Multi hop
                    }
                }
            }
        }
        private async Task ProcessServerLocalMessage(Message message)
        {
            if (message.User != null && message.Disconnect == true)
            {
                //Disconnect message
                if (!server.remoteusers.TryRemove(message.User, out _))
                {
                    //Is already disconnected
                    //Or wasn't connected at all
                }
            }
        }
        private void DisconnectServer()
        {
            if (!server.remoteservers.TryRemove(name, out _))
            {
                //Remote server is already removed
            }
            foreach (string user in server.remoteusers.Keys)
            {
                if (server.remoteusers.TryGetValue(user, out string? srv))
                {
                    if (srv == name)
                    {
                        if (!server.remoteusers.TryRemove(user, out _))
                        {
                            //Is already removed
                        }
                    }
                }
            }
        }
    }
}
