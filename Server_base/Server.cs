using Messages;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Server
{
    public partial class Server
    {
        public float SV = 1;
        private readonly TcpListener listener;
        public string name;
        private bool active;
        private readonly string ip;
        public ConcurrentDictionary<string, Client> clients; //Connected clients
        public ConcurrentDictionary<string, Client> remoteservers; //Connected remote servers
        public ConcurrentDictionary<string, ConcurrentQueue<Message>> messages; //Messages to be sent to users who's home server is this
        public ConcurrentDictionary<string, ConcurrentQueue<Message>> messages_server; //Messages to be sent to remote server
        public ConcurrentDictionary<string, string> remoteusers; //Users whos home server is this, but are connected to remote one
        public ConcurrentDictionary<string, Servers> servers; //Know servers
        public Server(string name, string IP, int port)
        {
            this.name = name.ToLower();
            ip = IP;
            listener = new TcpListener(IPAddress.Parse(ip), port);
            listener.Start();
            active = true;
            clients = new ConcurrentDictionary<string, Client>();
            messages = new ConcurrentDictionary<string, ConcurrentQueue<Message>>();
            remoteservers = new ConcurrentDictionary<string, Client>();
            messages_server = new ConcurrentDictionary<string, ConcurrentQueue<Message>>();
            remoteusers = new ConcurrentDictionary<string, string>();
            servers = new ConcurrentDictionary<string, Servers>();
            _ = Accept();
        }
        private async Task Accept()
        {
            //Loads data into memmory
            await LoadServers();
            while (active)
            {
                _ = new Client(this, await listener.AcceptTcpClientAsync());
            }
        }
        public async Task SendMessage(string user, Message message)
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
                    await SendMessageRemote(server, message);
                }
            }
            else
            {
                if (!AddMessages(user, message))
                {
                    //Don't know why
                }
                else {
                }
            }
        }
        public async Task SendMessageRemote(string server, Message message)
        {
            if (remoteservers.TryGetValue(server.ToLower(), out Client? srv))
            {
                //Already connected
                if (srv != null)
                {
                    await srv.SendMessage(message);
                }
            }
            else
            {
                var srv_data = GetServer(server);
                if (srv_data.Item1)
                {
                    //We found server IP and port
                    Client remote = new(this, server, srv_data.Item2, srv_data.Item3, srv_data.Item4, srv_data.Item5);
                    if (remoteservers.TryAdd(server.ToLower(), remote))
                    {
                        //Key already exsists
                        if(remoteservers.TryGetValue(server.ToLower(),out Client? cli))
                        {
                            if (cli != null)
                            {
                                //Disconnect previous one
                                //This will also delete it
                                await cli.Disconnect();
                                //Try once again
                                if (remoteservers.TryAdd(server.ToLower(), remote))
                                {
                                    //Don't know why
                                }
                            }
                        }
                    }
                    bool sent = await remote.SendMessage(message);
                    if (!sent)
                    {
                        //Console.WriteLine("Not sent to remote server");
                        //Not sent, save message
                        if (!AddMessages_server(server, message))
                        {
                            //Don't know why
                        }
                    }
                }
                else
                {
                    //We don't know this server
                    Console.WriteLine("No idea what to do");
                    if (!AddMessages_server(server, message))
                    {
                        //Don't know why
                    }
                    //to implement: asking all know servers, giving up
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
            } catch (Exception ex)
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
            } catch (Exception ex)
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
                listener.Stop();
                listener.Dispose();
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
            } catch (Exception ex)
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
                if(client.Key!= null)
                {
                    string[] names = client.Key.Split("@");
                    if (names[1] == server)
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
        public async Task WriteLog(Exception ex)
        {
            string log = DateTime.Now.ToString("d.M.yyyy. H:m:s") + " " + ex.ToString() + Environment.NewLine;
            try
            {
                await System.IO.File.AppendAllTextAsync("Server.log", log);
            } catch (Exception _)
            {
                Console.WriteLine("Can't save log to file.");
                Console.WriteLine(log);
            }
        }
    }
}
