using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using ConfigurationData;
using Messages;
using Server_interface;
using Sodium;
using Transport;

namespace Server_base
{
    public partial class Server : IServer
    {
        public int SV = 1;
        private readonly int HopCount = 10; //Max number of hops between servers
        private readonly TListener[] listeners;
        public string name;
        private bool active;
        private bool serversloaded = false;
        public ConcurrentDictionary<string, Client> clients; //Connected clients
        public ConcurrentDictionary<string, Client> remoteservers; //Connected remote servers
        public ConcurrentDictionary<string, DataHandler> messages; //DataHandlers for messages to be sent to users who's home server is this
        public ConcurrentDictionary<string, DataHandler> messages_server; //Messages to be sent to remote server
        public ConcurrentDictionary<string, string> remoteusers; //Users whos home server is this, but are connected to remote one
        public ConcurrentDictionary<string, Servers> servers; //Know servers
        public ImmutableList<Interface> interfaces;
        public KeyPair my;
        public TaskCompletionSource<bool> Closed { get; set; }

        public WriteLogAsync? Writelogasync { get; set; }

        public IServer CreateServer(string name, List<Interface> interfaces, KeyPair ecdh, WriteLogAsync? writelogasync)
        {
            return new Server(name, interfaces, ecdh, writelogasync);
        }
        public Server(string name, List<Interface> interfaces, KeyPair ecdh, WriteLogAsync? writelogasync)
        {
            this.name = name.ToLower();
            this.Writelogasync = writelogasync;
            active = true;
            clients = new ConcurrentDictionary<string, Client>();
            messages = new ConcurrentDictionary<string, DataHandler>();
            remoteservers = new ConcurrentDictionary<string, Client>();
            messages_server = new ConcurrentDictionary<string, DataHandler>();
            remoteusers = new ConcurrentDictionary<string, string>();
            servers = new ConcurrentDictionary<string, Servers>();
            List<TListener> listeners1 = [];
            my = ecdh;
            Closed = new();
            foreach (Interface iface in interfaces)
            {
                TListener listener = new(new TcpListener(IPAddress.Parse(iface.InterfaceIP), iface.Port));
                listener.Start();
                _ = Accept(listener, iface.InterfaceIP);
                listeners1.Add(listener);
            }
            listeners = [.. listeners1];
            this.interfaces = [.. interfaces];
            _ = LoadMessageDataHandlers();
        }
        private async Task Accept(TListener listener, string localip)
        {
            //Loads data into memmory
            if (!serversloaded)
            {
                await LoadServers();
                serversloaded = true;
            }
            while (active)
            {
                if (!active)
                {
                    break;
                }
                _ = new Client(this, await listener.AcceptAsync(), localip);
            }
        }
        /// <summary>
        /// Sends message to users for whom this is home server.
        /// </summary>
        /// <param name="user">Username.</param>
        /// <param name="message">Message.</param>
        /// <returns>Async Task.</returns>
        public async Task SendMessageThisServer(string user, Message message)
        {
            if (clients.TryGetValue(user.ToLower(), out Client? client))
            {
                if (client != null)
                {
                    //Client is on this server
                    await client.SendMessage(message);
                }
            }
            else if (remoteusers.TryGetValue(user.ToLower(), out string? server))
            {
                if (server != null)
                {
                    //Client is on remote server
                    await SendMessageServer(server, message);
                }
            }
            else
            {
                //Client is not connected and not on remote server
                //Save the message
                if (!await AddMessages(user, message))
                {
                    //Don't know why
                }
                else
                {
                }
            }
        }
        /// <summary>
        /// Sends message to users who's home server is other one.
        /// </summary>
        /// <param name="user">Username.</param>
        /// <param name="message">Message.</param>
        /// <returns>Async Task.</returns>
        public async Task SendMessageOtherServer(string user, Message message)
        {
            if (clients.TryGetValue(user, out Client? cli) && cli != null)
            {
                //Client is connected to this server
                await cli.SendMessage(message);
            }
            else
            {
                await SendMessageServer(StringProcessing.GetServer(user).ToString(), message);
            }
        }
        private async Task SendMessageKnownServer(string server, Message message)
        {
            if (remoteservers.TryGetValue(server, out Client? srv) && srv != null)
            {
                //We're already connected to this server
                await srv.SendMessage(message);
            }
            else
            {
                //We are not already connected
                var srvdata = GetServer(server);
                if (srvdata.Item1)
                {
                    //Known server
                    Client cli = await Client.CreateAsync(this, server, srvdata.Item2, srvdata.Item3, srvdata.Item4, srvdata.Item5);
                    if (!remoteservers.TryAdd(name, cli))
                    {
                        //Don't know why
                    }
                }
            }
        }
        /// <summary>
        /// Sends message to given server.
        /// </summary>
        /// <param name="server">Server name.</param>
        /// <param name="message">Message.</param>
        /// <returns>Async Task.</returns>
        public async Task SendMessageServer(string server, Message message, string? received = null)
        {
            if (remoteservers.TryGetValue(server, out Client? srv) && srv != null)
            {
                //We're already connected to this server
                await srv.SendMessage(message);
            }
            else
            {
                //We are not already connected
                var srvdata = GetServer(server);
                if (srvdata.Item1)
                {
                    //Known server
                    Client cli = await Client.CreateAsync(this, server, srvdata.Item2, srvdata.Item3, srvdata.Item4, srvdata.Item5);
                    if (!remoteservers.TryAdd(server, cli))
                    {
                        //Don't know why
                    }
                    await cli.SendMessage(message);
                }
                else
                {
                    //Unknown server
                    //Multi hop
                    if (message.Hop == null)
                    {
                        //This is first hop
                        message.Hop = 1;
                    }
                    else
                    {
                        //This is not first hop
                        message.Hop += 1;
                    }
                    if (message.Hop <= HopCount && received != null)
                    {
                        //Send to all known servers
                        foreach (string srvname in servers.Keys)
                        {
                            //Make sure we don't send to this server
                            //Also make sure we don't send back to server from which we received message
                            if (srvname != name && srvname != received)
                            {
                                await SendMessageKnownServer(srvname, message);
                            }
                        }
                    }
                    else if (received == null)
                    {
                        await WriteLog(new Exception("Received is null for multi hop."));
                    }
                }
            }
        }
        /// <summary>
        /// Returns data about known server.
        /// </summary>
        /// <param name="name">Name of the server.</param>
        /// <returns>(bool, localip, remoteip, remoteport, timeout)</returns>
        public (bool, string, string, int, int) GetServer(string name)
        {
            //get server by name
            if (servers.TryGetValue(name.ToLower(), out var server))
            {
                return (true, server.LocalIP, server.RemoteIP, server.RemotePort, server.TimeOut);
            }
            return (false, "", "", 0, 0);
        }
        /// <summary>
        /// Loads known servers from Servers.json file.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task LoadServers()
        {
            try
            {
                string jsonString = await System.IO.File.ReadAllTextAsync("Servers.json");
                await Task.Run(() =>
                 {
                     var servers_list = Servers.Deserialize(jsonString);
                     if (servers_list != null)
                     {
                         foreach (Servers server in servers_list)
                         {
                             server.Name = server.Name.ToLower();
                             servers.TryAdd(server.Name.ToLower(), server);
                         }
                     }
                 });
            }
            catch (Exception ex)
            {
                //Logging
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Saves known servers to Servers.json file.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task SaveServers()
        {
            try
            {
                /*string jsonString = await Task.Run(() =>
                {*/
                List<Servers> servers_list = [];
                foreach (var server in servers)
                {
                    server.Value.Name = server.Value.Name.ToLower();
                    servers_list.Add(server.Value);
                }
                /*return Servers.Serialize(servers_list.ToArray());
            });*/
                await System.IO.File.WriteAllTextAsync("Servers.json", Servers.Serialize([.. servers_list]));
            }
            catch (Exception ex)
            {
                //Logging
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Adds message to be sent to server into the file.
        /// </summary>
        /// <param name="server">Name of the server,</param>
        /// <param name="message">Message to be saved.</param>
        /// <returns>Async Task that completes with bool.</returns>
        public async Task<bool> AddMessages_server(string server, Message message)
        {
            server = server.ToLower();
            if (messages_server.TryGetValue(server, out DataHandler? handler) && handler != null)
            {
                return await handler.AppendMessage(message);
            }
            else
            {
                DataHandler handler1 = await DataHandler.CreateData(server, SV);
                await handler1.AppendMessage(message);
                if (!messages_server.TryAdd(server, handler1))
                {
                    //Key already exsists in dictionary
                    //This shouldn't happen
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        /// <summary>
        /// Adds message to be sent to user into the file.
        /// </summary>
        /// <param name="user">Name of the user,</param>
        /// <param name="message">Message to be saved.</param>
        /// <returns>Async Task that completes with bool.</returns>
        public async Task<bool> AddMessages(string user, Message message)
        {
            user = user.ToLower();
            if (messages.TryGetValue(user, out DataHandler? handler) && handler != null)
            {
                return await handler.AppendMessage(message);
            }
            else
            {
                DataHandler handler1 = await DataHandler.CreateData(user, SV);
                await handler1.AppendMessage(message);
                if (!messages.TryAdd(user, handler1))
                {
                    //Key already exsists in dictionary
                    //This shouldn't happen
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        /// <summary>
        /// Closes and disposes the server.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task Close()
        {
            try
            {
                active = false;
                List<Task> tasklist = [];
                foreach (TListener listener in listeners)
                {
                    listener.Stop();
                    listener.Dispose();
                }
                //Disconnect clients
                foreach (var client in clients)
                {
                    tasklist.Add(client.Value.Disconnect(true));
                }
                //Disconnect remore servers
                foreach (var remotes in remoteservers)
                {
                    tasklist.Add(remotes.Value.Disconnect(true));
                }
                //Delete remore users
                foreach (var remoteu in remoteusers)
                {
                    if (!remoteusers.TryRemove(remoteu))
                    {
                        //User wasn't even in the dictionary
                    }
                }
                //Save servers to the file
                tasklist.Add(SaveServers());
                //Delete DataHandlers for messages
                foreach (var message in messages)
                {
                    tasklist.Add(message.Value.Close());
                    if (!messages.TryRemove(message))
                    {
                        //Console.WriteLine("Error remove message.");
                    }
                }
                //Delete DataHandlers for messages for remote servers
                foreach (var rmessage in messages_server)
                {
                    tasklist.Add(rmessage.Value.Close());
                    if (!messages_server.TryRemove(rmessage))
                    {
                        //Console.WriteLine("Error remore message for other server.");
                    }
                }
                Servers.Unloading();
                await Task.WhenAll(tasklist);
                Closed.SetResult(true);
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Gets users whos home server is given one, but are connected to this server.
        /// </summary>
        /// <param name="server">Name of the server,</param>
        /// <returns>Formated string.</returns>
        public string? GetUsersServer(string server)
        {
            server = server.ToLower();
            List<string> users = [];
            foreach (var client in clients)
            {
                if (client.Key != null)
                {
                    if (MemoryExtensions.Equals(StringProcessing.GetServer(client.Key), server, StringComparison.OrdinalIgnoreCase))
                    {
                        users.Add(client.Key);
                    }
                }
            }
            if (users.Count > 0)
            {
                return string.Join(";", [.. users]);
            }
            return null;
        }
        /// <summary>
        /// Gets inteface by it's IP address.
        /// </summary>
        /// <param name="InterfaceIP">Interface IP address.</param>
        /// <returns>(piblic IP, port)</returns>
        public async Task<(string, int)> GetInterfacebyIP(string InterfaceIP)
        {
            return await Task.Run(() =>
            {
                foreach (Interface iface in interfaces)
                {
                    if (iface.InterfaceIP == InterfaceIP)
                    {
                        return (iface.IP, iface.Port);
                    }
                }
                return ("", 0);
            });
        }
        /// <summary>
        /// Loads DataHandlers for all saved messages.
        /// </summary>
        /// <returns>Async Task.</returns>
        private async Task LoadMessageDataHandlers()
        {
            try
            {
                if (Directory.Exists("Data"))
                {
                    foreach (string file in Directory.GetFiles("Data"))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);
                        if (StringProcessing.IsUser(name))
                        {
                            //User
                            messages.TryAdd(name, await DataHandler.CreateData(name, SV));
                        }
                        else
                        {
                            //Server
                            messages_server.TryAdd(name, await DataHandler.CreateData(name, SV));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Writes exception into Server.log file. Sends exception to remote.
        /// </summary>
        /// <param name="ex">Exception to be saved and send.</param>
        /// <returns>Async Task.</returns>
        public async Task WriteLog(Exception ex)
        {
            string log = DateTime.Now.ToString("d.M.yyyy. H:m:s") + " " + ex.ToString() + Environment.NewLine;
            try
            {
                await System.IO.File.AppendAllTextAsync("Server.log", log);
                if (Writelogasync != null)
                {
                    await Writelogasync(log);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Can't save log to file.");
                Console.WriteLine(log);
            }
        }
    }
}
