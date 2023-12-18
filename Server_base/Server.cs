using Configuration;
using Messages;
using Sodium;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using Transport;

namespace Server
{
    public partial class Server
    {
        public int SV = 1;
        private readonly int HopCount = 10; //Max number of hops between servers
        private readonly TListener[] listeners;
        public string name;
        private bool active;
        private bool serversloaded = false;
        public ConcurrentDictionary<string, Client> clients; //Connected clients
        public ConcurrentDictionary<string, Client> remoteservers; //Connected remote servers
        public ConcurrentDictionary<string, ConcurrentQueue<Message>> messages; //Messages to be sent to users who's home server is this
        public ConcurrentDictionary<string, ConcurrentQueue<Message>> messages_server; //Messages to be sent to remote server
        public ConcurrentDictionary<string, string> remoteusers; //Users whos home server is this, but are connected to remote one
        public ConcurrentDictionary<string, Servers> servers; //Know servers
        public ImmutableList<Interface> interfaces;
        public KeyPair my;
        public Server(string name, List<Interface> interfaces, KeyPair ecdh)
        {
            this.name = name.ToLower();
            active = true;
            clients = new ConcurrentDictionary<string, Client>();
            messages = new ConcurrentDictionary<string, ConcurrentQueue<Message>>();
            remoteservers = new ConcurrentDictionary<string, Client>();
            messages_server = new ConcurrentDictionary<string, ConcurrentQueue<Message>>();
            remoteusers = new ConcurrentDictionary<string, string>();
            servers = new ConcurrentDictionary<string, Servers>();
            List<TListener> listeners1 = [];
            my = ecdh;
            foreach (Interface iface in interfaces)
            {
                TListener listener = new(new TcpListener(IPAddress.Parse(iface.InterfaceIP), iface.Port));
                listener.Start();
                _ = Accept(listener, iface.InterfaceIP);
                listeners1.Add(listener);
            }
            listeners = [.. listeners1];
            this.interfaces = [.. interfaces];
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
                if (!AddMessages(user, message))
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
                await SendMessageServer(StringProcessing.GetServer(user), message);
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
        public async Task SendMessageServer(string server, Message message)
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
                    if (message.Hop <= HopCount)
                    {
                        //Send to all known servers
                        foreach (string srvname in servers.Keys)
                        {
                            //Make sure we don't send to this server
                            if (srvname != name)
                            {
                                await SendMessageKnownServer(srvname, message);
                            }
                        }
                    }
                }
            }
        }
        public (bool, string, string, int, int) GetServer(string name)
        {
            //get server by name
            if (servers.TryGetValue(name.ToLower(), out var server))
            {
                return (true, server.LocalIP, server.RemoteIP, server.RemotePort, server.TimeOut);
            }
            return (false, "", "", 0, 0);
        }
        public async Task LoadServers()
        {
            try
            {
                //Load servers from Servers.json
                string jsonString = await System.IO.File.ReadAllTextAsync("Servers.json");
                await Task.Run(() =>
                 {
                     var servers_list = JsonSerializer.Deserialize<Servers[]>(jsonString);
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
        public async Task SaveServers()
        {
            try
            {
                string jsonString = await Task.Run(() =>
                {
                    List<Servers> servers_list = [];
                    foreach (var server in servers)
                    {
                        server.Value.Name = server.Value.Name.ToLower();
                        servers_list.Add(server.Value);
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
        public bool AddMessages_server(string server, Message message)
        {
            if (messages_server.TryGetValue(server.ToLower(), out var mssg))
            {
                mssg.Enqueue(message);
                return true;
            }
            else
            {
                mssg = new ConcurrentQueue<Message>();
                mssg.Enqueue(message);
                if (!messages_server.TryAdd(server, mssg))
                {
                    //Key already exsists in dictionary
                    //This shouldn't happen
                    return false;
                }
                return true;
            }
        }
        public bool AddMessages(string user, Message message)
        {
            if (messages.TryGetValue(user.ToLower(), out var mssg))
            {
                mssg.Enqueue(message);
                return true;
            }
            else
            {
                mssg = new ConcurrentQueue<Message>();
                mssg.Enqueue(message);
                if (!messages.TryAdd(user.ToLower(), mssg))
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
        public async Task Close()
        {
            try
            {
                active = false;
                foreach (TListener listener in listeners)
                {
                    listener.Stop();
                    listener.Dispose();
                }
                //Disconnect clients
                foreach (var client in clients)
                {
                    await client.Value.Disconnect(true);
                }
                //Disconnect remore servers
                foreach (var remotes in remoteservers)
                {
                    await remotes.Value.Disconnect(true);
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
                await SaveServers();
                //Do something with messages
                //For now, just delete
                foreach (var message in messages)
                {
                    if (!messages.TryRemove(message))
                    {
                        Console.WriteLine("Error remove message.");
                    }
                }
                //Do something with messages for remote servers
                foreach (var rmessage in messages_server)
                {
                    if (!messages_server.TryRemove(rmessage))
                    {
                        Console.WriteLine("Error remore message for other server.");
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        public string? GetUsersServer(string server)
        {
            server = server.ToLower();
            List<string> users = [];
            foreach (var client in clients)
            {
                if (client.Key != null)
                {
                    if (StringProcessing.GetServer(client.Key) == server)
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
        public static async Task WriteLog(Exception ex)
        {
            string log = DateTime.Now.ToString("d.M.yyyy. H:m:s") + " " + ex.ToString() + Environment.NewLine;
            try
            {
                await System.IO.File.AppendAllTextAsync("Server.log", log);
            }
            catch (Exception)
            {
                Console.WriteLine("Can't save log to file.");
                Console.WriteLine(log);
            }
        }
    }
}
