using Messages;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Server
{
    public partial class Server
    {
<<<<<<< Updated upstream
        public int SV = 1;
        private TcpListener listener;
=======
        public float SV = 1;
        private readonly TcpListener listener;
>>>>>>> Stashed changes
        public string name;
        private bool active;
        private readonly string ip;
        public ConcurrentDictionary<string, Client> clients; //Connected clients
        public ConcurrentDictionary<string, Client> remoteservers; //Connected remote servers
        public ConcurrentDictionary<string, ConcurrentBag<Message>> messages; //Messages to be sent to users who's home server is this
        public ConcurrentDictionary<string, ConcurrentBag<Message>> messages_server; //Messages to be sent to remote server
        public ConcurrentDictionary<string, string> remoteusers; //Users whos home server is this, but are connected to remote one
        public ConcurrentDictionary<string, Servers> servers; //Know servers
        public Server(string name, string IP, int port)
        {
            this.name = name.ToLower();
            this.ip = IP;
<<<<<<< Updated upstream
            listener = new TcpListener(IPAddress.Parse(IP), port);
=======
            listener = new TcpListener(IPAddress.Parse(ip), port);
>>>>>>> Stashed changes
            listener.Start();
            active = true;
            clients = new ConcurrentDictionary<string, Client>();
            messages = new ConcurrentDictionary<string, ConcurrentBag<Message>>();
            remoteservers = new ConcurrentDictionary<string, Client>();
            messages_server = new ConcurrentDictionary<string, ConcurrentBag<Message>>();
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
<<<<<<< Updated upstream
                new Client(this, await listener.AcceptTcpClientAsync());
=======
                _ = new Client(this, await listener.AcceptTcpClientAsync());
>>>>>>> Stashed changes
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
                    Console.WriteLine("Error adding to messages list.");
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
<<<<<<< Updated upstream
                    Client remote = new Client(this, server, srv_data.Item2, srv_data.Item3, srv_data.Item4, srv_data.Item5);
                    if(remoteservers.TryAdd(server.ToLower(), remote))
=======
                    Client remote = new(this, server, srv_data.Item2, srv_data.Item3, srv_data.Item4, srv_data.Item5);
                    if (remoteservers.TryAdd(server.ToLower(), remote))
>>>>>>> Stashed changes
                    {
                        Console.WriteLine("Error add client.");
                    }
                    bool sent = await remote.SendMessage(message);
                    if (!sent)
                    {
                        Console.WriteLine("Not sent to remote server");
                        //Not sent, save message
                        if (!AddMessages_server(server, message))
                        {
                            Console.WriteLine("Error adding to messages_server list.");
                        }
                    }
                }
                else
                {
                    //We don't know this server
                    Console.WriteLine("No idea what to do");
                    if (!AddMessages_server(server, message))
                    {
                        Console.WriteLine("Error adding to messages_server list.");
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
        public async Task SaveServers()
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
        public bool AddMessages_server(string server, Message message)
        {
            if (messages_server.TryGetValue(server.ToLower(), out var mssg))
            {
<<<<<<< Updated upstream
                mssg.Add(message);
=======
                _ = mssg.Append(message);
>>>>>>> Stashed changes
                return true;
            }
            else
            {
<<<<<<< Updated upstream
                mssg = new ConcurrentBag<Message>();
                mssg.Add(message);
=======
                mssg = new ConcurrentQueue<Message>();
                _ = mssg.Append(message);
>>>>>>> Stashed changes
                if (!messages_server.TryAdd(server, mssg))
                {
                    return false;
                }
                return true;
            }
        }
        public bool AddMessages(string user, Message message)
        {
            if (messages.TryGetValue(user.ToLower(), out var mssg))
            {
<<<<<<< Updated upstream
                mssg.Add(message);
=======
                _ = mssg.Append(message);
>>>>>>> Stashed changes
                return true;
            }
            else
            {
<<<<<<< Updated upstream
                mssg = new ConcurrentBag<Message>();
                mssg.Add(message);
=======
                mssg = new ConcurrentQueue<Message>();
                _ = mssg.Append(message);
>>>>>>> Stashed changes
                if (!messages.TryAdd(user.ToLower(), mssg))
                {
                    return false;
                }
                return true;
            }
        }
        public async Task Close()
        {
            active = false;
            listener.Stop();
<<<<<<< Updated upstream
=======
            listener.Dispose();
>>>>>>> Stashed changes
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
                    Console.WriteLine("Error remove remote user.");
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
    }
}
