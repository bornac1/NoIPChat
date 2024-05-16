using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using ConfigurationData;
using Messages;
using Server_interface;
using Sodium;
using Transport;

namespace Server_base
{
    /// <summary>
    /// Server.
    /// </summary>
    public partial class Server : IServer
    {
        /// <summary>
        /// Server version.
        /// </summary>
        public int SV = 1;
        private readonly int HopCount = 10; //Max number of hops between servers
        private readonly TListener[] listeners;
        /// <summary>
        /// Name of the Server.
        /// </summary>
        public string name;
        private bool active;
        private bool serversloaded = false;
        /// <summary>
        /// Connected Clients.
        /// </summary>
        public ConcurrentDictionary<string, Client> clients;
        /// <summary>
        /// Connected remote servers.
        /// </summary>
        public ConcurrentDictionary<string, Client> remoteservers;
        /// <summary>
        /// DataHandlers for messages to be sent to users who's home server is this.
        /// </summary>
        public ConcurrentDictionary<string, DataHandler> messages;
        /// <summary>
        /// Messages to be sent to remote server
        /// </summary>
        public ConcurrentDictionary<string, DataHandler> messages_server;
        /// <summary>
        /// Users whos home server is this, but are connected to remote one.
        /// </summary>
        public ConcurrentDictionary<string, string> remoteusers;
        /// <summary>
        /// Know servers.
        /// </summary>
        public ConcurrentDictionary<string, Servers> servers;
        /// <summary>
        /// Interfaces currently used.
        /// </summary>
        public ImmutableList<Interface> interfaces;
        /// <summary>
        /// Sodium ECDH KeyPair.
        /// </summary>
        public KeyPair my;
        private readonly string logfile;
        /// <summary>
        /// PluginInfos for loaded plugins.
        /// </summary>
        public List<PluginInfo> plugins;
        private readonly AssemblyLoadContext context;
        /// <summary>
        /// Returns true when Server is fully closed.
        /// </summary>
        public TaskCompletionSource<bool> Closed { get; set; }
        /// <summary>
        /// Delegate for async log writing.
        /// </summary>
        public WriteLogAsync? Writelogasync { get; set; }
        /// <summary>
        /// Server constructor.
        /// </summary>
        /// <param name="name">Name of the server.</param>
        /// <param name="interfaces">List of interfaces.</param>
        /// <param name="ecdh">Sodium ECDH KeyPair.</param>
        /// <param name="writelogasync">Delegate for async log writing.</param>
        /// <param name="logfile">Path to custom logfile.</param>
        /// <param name="context">AssemblyLoadContext used for loading plugins. Should be the same as where Server_base is loaded.</param>
        public Server(string name, List<Interface> interfaces, KeyPair ecdh, WriteLogAsync? writelogasync, string? logfile, AssemblyLoadContext context)
        {
            this.context = context;

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
            plugins = [];
            my = ecdh;
            if (!string.IsNullOrEmpty(logfile))
            {
                this.logfile = logfile;
            }
            else
            {
                this.logfile = "Server.log";
            }
            LoadPlugins();
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
            foreach (PluginInfo plugininfo in plugins)
            {
                try
                {
                    plugininfo.Plugin.ServerStart();
                }
                catch (Exception ex)
                {
                    if (ex is NotImplementedException)
                    {
                        //Disregard
                    }
                    else
                    {
                        try
                        {
                            plugininfo.Plugin.WriteLog(ex);
                        }
                        catch
                        {
                            //Disregard
                        }
                    }
                }
            }
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
                Client client = new(this, await listener.AcceptAsync(), localip);
                foreach (PluginInfo plugininfo in plugins)
                {
                    try
                    {
                        await plugininfo.Plugin.ClientAcceptedAsync(client);
                    }
                    catch (Exception ex)
                    {
                        if (ex is NotImplementedException)
                        {
                            //Disregard
                        }
                        else
                        {
                            try
                            {
                                plugininfo.Plugin.WriteLog(ex);
                            }
                            catch
                            {
                                //Disregard
                            }
                        }
                    }
                }
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
        /// <param name="received">Name of the server from which message was received.</param>
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
            var info = (false, "", "", 0, 0);
            if (servers.TryGetValue(name.ToLower(), out var server))
            {
                info = (true, server.LocalIP, server.RemoteIP, server.RemotePort, server.TimeOut);
                foreach (PluginInfo plugininfo in plugins)
                {
                    try
                    {
                        plugininfo.Plugin.GetServerInfo(server.LocalIP, server.RemoteIP, server.RemotePort, server.TimeOut);
                    }
                    catch (Exception ex)
                    {
                        if (ex is NotImplementedException)
                        {
                            //Disregard
                        }
                        else
                        {
                            try
                            {
                                plugininfo.Plugin.WriteLog(ex);
                            }
                            catch
                            {
                                //Disregard
                            }
                        }
                    }
                }
            }
            foreach (PluginInfo plugininfo in plugins)
            {
                try
                {
                    info = plugininfo.Plugin.ReturnServerInfo(name);
                }
                catch (Exception ex)
                {
                    if (ex is NotImplementedException)
                    {
                        //Disregard
                    }
                    else
                    {
                        try
                        {
                            plugininfo.Plugin.WriteLog(ex);
                        }
                        catch
                        {
                            //Disregard
                        }
                    }
                }
            }
            return info;
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
            foreach (PluginInfo plugininfo in plugins)
            {
                try
                {
                    await plugininfo.Plugin.ServersLoadAsync();
                }
                catch (Exception ex)
                {
                    if (ex is NotImplementedException)
                    {
                        //Disregard
                    }
                    else
                    {
                        try
                        {
                            plugininfo.Plugin.WriteLog(ex);
                        }
                        catch
                        {
                            //Disregard
                        }
                    }
                }
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
            foreach (PluginInfo plugininfo in plugins)
            {
                try
                {
                    await plugininfo.Plugin.ServersSaveAsync();
                }
                catch (Exception ex)
                {
                    if (ex is NotImplementedException)
                    {
                        //Disregard
                    }
                    else
                    {
                        try
                        {
                            plugininfo.Plugin.WriteLog(ex);
                        }
                        catch
                        {
                            //Disregard
                        }
                    }
                }
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
                await System.IO.File.AppendAllTextAsync(logfile, log);
                if (Writelogasync != null)
                {
                    await Writelogasync(log);
                }
                foreach (PluginInfo plugininfo in plugins)
                {
                    try
                    {
                        plugininfo.Plugin.ServerLog(ex);
                    }
                    catch (Exception ex1)
                    {
                        if (ex1 is NotImplementedException)
                        {
                            //Disregard
                        }
                        else
                        {
                            try
                            {
                                plugininfo.Plugin.WriteLog(ex);
                            }
                            catch
                            {
                                //Disregard
                            }
                        }
                    }
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Can't save log to file {logfile}.");
                Console.WriteLine(log);
                Console.WriteLine(ex2.ToString());
            }
        }
        private static void UnpackZip(string zipFilePath, string extractPath)
        {
            Directory.CreateDirectory(extractPath);
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string entryFullName = Path.Combine(extractPath, entry.FullName);
                string? directory = Path.GetDirectoryName(entryFullName);
                if (directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                entry.ExtractToFile(entryFullName, true);
            }
        }
        private void UnpackPlugins()
        {
            try
            {
                Directory.CreateDirectory("Plugins");
                string[] files = Directory.GetFiles("Plugins");
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).Equals(".nip", StringComparison.OrdinalIgnoreCase))
                    {
                        UnpackZip(file, Path.Combine("Plugins", Path.GetFileNameWithoutExtension(file)));
                        System.IO.File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex).Wait();
            }
        }
        /// <summary>
        /// Loads plugins.
        /// </summary>
        public void LoadPlugins()
        {
            UnpackPlugins();
            try
            {
                Directory.CreateDirectory("Plugins");
                string[] pluginsnames = Directory.GetDirectories("Plugins");
                foreach (string name in pluginsnames)
                {
                    try
                    {
                        if (Verify(Path.GetFullPath(name)))
                        {
                            string pluginname = Path.GetFileName(name);
                            string name1 = pluginname + ".dll";
                            Assembly asm = context.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(name, name1)));
                            Type? type = asm.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface).FirstOrDefault();
                            if (type != null)
                            {
                                var instance = Activator.CreateInstance(type);
                                if (instance != null)
                                {
                                    PluginInfo plugininfo = new()
                                    {
                                        Name = pluginname,
                                        Assembly = asm,
                                        Plugin = (IPlugin)instance
                                    };
                                    plugininfo.Plugin.Server = this;
                                    try
                                    {
                                        plugininfo.Plugin.Initialize();
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is NotImplementedException)
                                        {
                                            //Disregard
                                        }
                                        else
                                        {
                                            try
                                            {
                                                plugininfo.Plugin.WriteLog(ex);
                                            }
                                            catch
                                            {
                                                //Disregard
                                            }
                                        }
                                    }
                                    plugins.Add(plugininfo);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Plugin signature error");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex).Wait();
            }
        }
    }
}
