﻿using MessagePack;
using Messages;
using System.Net;
using Transport;

namespace Server_base
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
            if (aeskey != null)
            {
                message = Encryption.DecryptMessage(message, aeskey);
            }
            if (message.Users != null)
            {
                //Users message
                await ProcessUsers(message.Users);
            }
            else if (message.User != null && message.Pass != null)
            {
                //Login message
                await ProcessServerLoginMessage(message);
            }
            else if (message.Receiver != null && message.Auth != null)
            {
                //Auth message
                await ProcessServerAuthMessage(message);
            }
            else if (message.Sender != null && message.Receiver != null)
            {
                //Mesage to be relayed
                await ProcessServerRelayMessage(message);
            }
            else if (message.Receiver != null && message.Receiver == server.name)
            {
                //Message for this server
                ProcessServerLocalMessage(message);
            }
        }
        private async Task ProcessServerWelcomeMessage(Message message)
        {
            if (message.Name != null && message.Data != null)
            {
                name = message.Name.ToLower();
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
                foreach (string user in StringProcessing.GetUsersServer(users))
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
            if (message.Receiver != null)
            {
                if (MemoryExtensions.Equals(StringProcessing.GetServer(message.Receiver), server.name, StringComparison.OrdinalIgnoreCase))
                {
                    //This is message for user who's home server is this one
                    await server.SendMessageThisServer(message.Receiver, message);
                }
                else
                {
                    //This is message for user who's home server is other one
                    await server.SendMessageOtherServer(message.Receiver, message);
                }
            }
        }
        private async Task ProcessServerLoginMessage(Message message)
        {
            if (message.User != null && message.Pass != null)
            {
                string srv = StringProcessing.GetServer(message.User).ToString();
                if (MemoryExtensions.Equals(srv, server.name, StringComparison.OrdinalIgnoreCase))
                {
                    //Users home server is this one

                    //Authenticate
                    if (message.Pass != null)
                    {
                        await SendMessage(new Message()
                        {
                            Sender = server.name,
                            Receiver = message.User,
                            Auth = true
                        });
                        if (!server.remoteusers.TryAdd(message.User, name))
                        {
                            //Already exists
                        }
                    }
                    await SendAllMessagesRemoteUser(message.User);
                }
                else
                {
                    //Users home server is other one
                    //Multi hop
                    message.Sender = server.name;
                    message.Receiver = srv;
                    await server.SendMessageServer(srv, message, name);
                }
            }
        }
        private async Task ProcessServerAuthMessage(Message message)
        {
            if (message.Receiver != null)
            {
                string srv = StringProcessing.GetServer(message.Receiver).ToString();
                if (!MemoryExtensions.Equals(srv, server.name, StringComparison.OrdinalIgnoreCase))
                {
                    //Just to make sure
                    if (server.clients.TryGetValue(message.Receiver, out Client? cli))
                    {
                        //User is connected to this server
                        await cli.SendMessage(message);
                    }
                    else
                    {
                        //User is not connected to this server
                        //Multi hop
                        await server.SendMessageServer(srv, message, name);
                    }
                }
            }
        }
        private void ProcessServerLocalMessage(Message message)
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
        private async Task DisconnectServer(bool force = false)
        {
            if (force || ReconnectTimer?.Interval > ReconnectTimeOut)
            {
                if (connected)
                {
                    Message message1 = new()
                    {
                        Disconnect = true
                    };
                    await SendMessage(message1);
                }
                connected = false;
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
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                }
            }
            else if (isremote && ReconnectTimer != null && ReconnectTimer.Interval < ReconnectTimeOut)
            {
                //Reconnect only if this is connection to remote server
                ReconnectTimer.Start();
            }
        }
        private async void ReconnectServer(Object? source, System.Timers.ElapsedEventArgs e)
        {
            //Dispose old TClient
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
            //We have server name
            if (name != string.Empty)
            {
                var srv = server.GetServer(name);
                if (srv.Item1)
                {
                    //Found server informations
                    //Create new TClient
                    client = new TClient(new TcpClient(IPEndPoint.Parse(localip)));
                    await Connect(srv.Item3, srv.Item4);
                    if (ReconnectTimer != null)
                    {
                        if (!connected)
                        {
                            ReconnectTimer.Interval *= 2;
                        }
                        else
                        {
                            ReconnectTimer.Stop();
                            ReconnectTimer.Interval = InitialReconnectInterval;
                        }
                    }
                }
                else
                {
                    await DisconnectServer(true);
                }
            }
        }
    }
}
