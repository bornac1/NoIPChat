﻿using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using Messages;
using Sodium;
using Transport;

namespace Client
{
    /// <summary>
    /// Client class.
    /// </summary>
    public partial class Client
    {
        /// <summary>
        /// Client version.
        /// </summary>
        public Messages.Version CV = "0.5.2";
        private readonly string runtime = RuntimeInformation.RuntimeIdentifier;
        /// <summary>
        /// Connected flag.
        /// </summary>
        public bool connected = false;
        /// <summary>
        /// Messages to be sent. Used for error recovery.
        /// </summary>
        public ConcurrentQueue<Messages.Message> messages_snd;
        /// <summary>
        /// Username.
        /// </summary>
        public string? Username;
        /// <summary>
        /// Password.
        /// </summary>
        public string? Password;
        /// <summary>
        /// Server used for connection.
        /// </summary>
        public Servers? Server;
        /// <summary>
        /// TClient object.
        /// </summary>
        public TClient client;
        /// <summary>
        /// Known servers BindingSource.
        /// </summary>
        public BindingSource servers;
        /// <summary>
        /// Used as indicator for chat message showing.
        /// </summary>
        public TaskCompletionSource<bool> ischatready = new();
        /// <summary>
        /// Main object.
        /// </summary>
        public Main main;
        private bool disconnectstarted;
        private readonly byte[] bufferl = new byte[sizeof(int)];
        private byte[] bufferm = new byte[1024];
        private readonly KeyPair my;
        private byte[]? aeskey;
        /// <summary>
        /// Used as indicator for authentication.
        /// </summary>
        public TaskCompletionSource<bool> auth = new();
        private readonly System.Timers.Timer ReconnectTimer;
        private const double ReconnectTimeOut = 60000;//60 seconds
        private const double InitialReconnectInterval = 15;
        /// <summary>
        /// PluginInfos for loaded plugins.
        /// </summary>
        public ConcurrentList<PluginInfo> plugins;
        private readonly ConcurrentList<ToolStripMenuItem> pluginmenuitems;
        private readonly Harmony harmony;
        private readonly string? clientpath;
        /// <summary>
        /// Client constructor.
        /// </summary>
        /// <param name="main">Main object.</param>
        public Client(Main main)
        {
            this.main = main;
            disconnectstarted = false;
            ReconnectTimer = new(InitialReconnectInterval);
            ReconnectTimer.Elapsed += Reconnect;
            messages_snd = [];
            servers = [];
            plugins = [];
            pluginmenuitems = [];
            harmony = new Harmony("patcher");
            clientpath = Path.GetDirectoryName(AppContext.BaseDirectory);
            _ = LoadPlugins();
            my = Encryption.GenerateECDH();
            _ = LoadServers();
            client = new TClient(new TcpClient(new IPEndPoint(IPAddress.Any, 0)));
        }
        /// <summary>
        /// Connects client to server.
        /// </summary>
        /// <param name="srv">Servers object.</param>
        /// <returns>Async Task.</returns>
        public async Task Connect(Servers srv)
        {
            Server = srv;
            try
            {
                client = new TClient(new TcpClient(new IPEndPoint(IPAddress.Any, 0)));
                await client.ConnectAsync(IPAddress.Parse(srv.IP), srv.Port);
                connected = true;
                //Send public key message
                await SendMessage(new Messages.Message()
                {
                    PublicKey = my.PublicKey
                });
                await ReceiveKey();
            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //Error connecting
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
                //Clean all
                await Disconnect();
            }
        }
        private async Task ReceiveKey()
        {
            try
            {
                int length = await ReadLength();
                ReadOnlyMemory<byte> data = await ReadData(length);
                Messages.Message message = await Processing.Deserialize(data);
                await ProcessMessage(message);
            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //assume disconnection
                    await Disconnect();
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                    //Clean all
                    await Disconnect();
                }
            }
        }
        private async Task Receive()
        {
            while (connected)
            {
                try
                {
                    int length = await ReadLength();
                    ReadOnlyMemory<byte> data = await ReadData(length);
                    Messages.Message message = await Processing.Deserialize(data);
                    await ProcessMessage(message);
                }
                catch (Exception ex)
                {
                    if (ex is TransportException)
                    {
                        //assume disconnection
                    }
                    else if (ex is ObjectDisposedException)
                    {
                        //Already disposed
                    }
                    else
                    {
                        //Logging
                        await WriteLog(ex);
                    }
                    //Clean all
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
            Handlebufferm(length);
            int totalread = 0;
            while (totalread < length)
            {
                int read = await client.ReceiveAsync(bufferm, totalread, length - totalread);
                totalread += read;
            }
            return new ReadOnlyMemory<byte>(bufferm, 0, totalread);
        }
        /// <summary>
        /// Login user.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <returns>Async Task.</returns>
        public async Task Login(string username, string password)
        {
            Messages.Message message = new();
            Username = username.ToLower();
            Password = password;
            message.CV = CV;
            message.User = Username;
            message.Pass = Encoding.UTF8.GetBytes(Password);
            if (await SendMessage(message))
            {
                _ = Receive();
            }
        }
        /// <summary>
        /// Sedns message.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <returns>Async Task that completes with true if sent, false if not.</returns>
        public async Task<bool> SendMessage(Messages.Message message)
        {
            bool msgerror = false;
            try
            {
                //Encrypt message
                if (aeskey != null)
                {
                    message = Encryption.EncryptMessage(message, aeskey);
                }
                byte[]? data = await Processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
                    await client.SendAsync(length);
                    await client.SendAsync(data);
                    return true;
                }
                else
                {
                    //Message error
                    msgerror = true;
                }
            }
            catch (Exception ex)
            {
                if (ex is TransportException)
                {
                    //Assume disconnection
                    //Save message to be sent later
                    if (!msgerror)
                    {
                        messages_snd.Enqueue(message);
                    }
                    else
                    {
                        //Message error
                        //Give up
                    }
                }
                else if (ex is ObjectDisposedException)
                {
                    //Already disposed
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
            }
            await Disconnect();
            return false;
        }
        /// <summary>
        /// Gets informations about server.
        /// </summary>
        /// <param name="name">Name of the server.</param>
        /// <returns>Server IP as string, port as int.</returns>
        public (string?, int) GetServer(string name)
        {
            name = name.ToLower();
            foreach (Servers server in servers)
            {
                if (server.Name == name)
                {
                    return (server.IP, server.Port);
                }
            }
            return (null, 0);
        }
        private async Task ProcessMessage(Messages.Message message)
        {
            foreach (PluginInfo plugininfo in plugins)
            {
                try
                {
                    await plugininfo.Plugin.MessageReceived(message);
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
                            await plugininfo.Plugin.WriteLog(ex);
                        }
                        catch
                        {
                            //Disregard
                        }
                    }
                }
            }
            if (aeskey != null)
            {
                message = Encryption.DecryptMessage(message, aeskey);
            }
            if (message.PublicKey != null)
            {
                //We have a public key from server
                aeskey = Encryption.GenerateAESKey(my, message.PublicKey);
            }
            else if (message.Auth == true)
            {
                //User is authenticated
                auth.TrySetResult(true);
                //auth = true;
            }
            else if (message.Auth == false)
            {
                auth.TrySetResult(false);
                //auth = false;
            }
            else if (message.Update == true)
            {
                //Received update package
                await Update(message);
            }
            else if (message.Msg != null || message.Data != null)
            {
                await HandleMessage(message);
            }
            if (message.CVU != null && message.CVU > CV && message.Update != true)
            {
                //Higher version available
                await RequestUpdate();
            }
        }
        private async Task Update(Messages.Message message)
        {
            try
            {
                if (message.CVU != null)
                {
                    if (message.CVU == CV)
                    {
                        //Same version, there is no update
                    }
                    else if (message.CVU > CV && message.Data != null)
                    {
                        //We received update package
                        Messages.File file = await Messages.Processing.DeserializeFile(message.Data);
                        if (file.Name != null && file.Content != null)
                        {
                            Directory.CreateDirectory("Download");
                            string path = Path.Combine("Download", file.Name);
                            await System.IO.File.WriteAllBytesAsync(path, file.Content);
                            if (file.Name.Contains("patch", StringComparison.OrdinalIgnoreCase))
                            {
                                //Patch
                                await LoadPatch(path);
                                System.IO.File.Delete(path);
                            }
                            else
                            {
                                //Update
                                MessageBox.Show("New version of Client received from Server");
                                PrepareUpdate(path);
                                System.IO.File.Delete(path);
                            }
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
        /// Requests update from server.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task RequestUpdate()
        {
            await SendMessage(new() { CV = CV, Update = true, Runtime = runtime });
        }
        /// <summary>
        /// Handles received message.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <returns>Async Task.</returns>
        public async Task HandleMessage(Messages.Message message)
        {

            if (main.chat != null && await ischatready.Task && message.Msg != null)
            {
                //Chat is ready
                //Check if it has file
                if (message.IsFile == true)
                {
                    await main.chat.SaveFile(message.Data);
                }
                //Display message
                main.chat.display.Invoke(() => main.chat.DisplayMessage(message));
            }
        }
        /// <summary>
        /// Disconnects client.
        /// </summary>
        /// <param name="force">True if forced, false if connection failed.</param>
        /// <returns>Async Task.</returns>
        public async Task Disconnect(bool force = false)
        {
            if (!disconnectstarted)
            {
                disconnectstarted = true;
                try
                {
                    connected = false;
                    if (force || Server == null || ReconnectTimer.Interval > ReconnectTimeOut)
                    {
                        //Disconnect by clicking button
                        await SendMessage(new Messages.Message() { CV = CV, Disconnect = true });
                        if (client != null)
                        {
                            client.Close();
                            client.Dispose();
                        }
                        //start new client
                        foreach (ToolStripMenuItem item in pluginmenuitems)
                        {
                            main.mainmenu.Items.Remove(item);
                        }
                        main.client = new Client(main);
                        await main.client.LoadServers();
                        //close all
                        main.CloseDisconnect(force);
                        main.ManipulateMenue(false);
                    }
                    else if (!force && ReconnectTimer.Interval < ReconnectTimeOut)
                    {
                        //Disconnect due to connection error
                        //Try reconnect if timer valid
                        ReconnectTimer.Start();
                    }
                }
                catch (Exception ex)
                {
                    //Logging
                    await WriteLog(ex);
                }
            }
        }
        private async void Reconnect(Object? source, System.Timers.ElapsedEventArgs e)
        {
            //Dispose old TClient
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
            //Reset
            disconnectstarted = false;
            if (Server != null)
            {
                await Connect(Server.Value);
                if (!connected)
                {
                    ReconnectTimer.Interval *= 2;
                }
                else
                {
                    ReconnectTimer.Stop();
                    ReconnectTimer.Interval = InitialReconnectInterval;
                    if (Username != null && Password != null)
                    {
                        //We can login back
                        await Login(Username, Password);
                    }
                    else
                    {
                        //We can't login back
                        //Disconnect and reset menue
                        await Disconnect(true);
                    }
                }
            }
        }
        /// <summary>
        /// Loads servers from file.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task LoadServers()
        {
            try
            {
                //Load servers from Servers.json
                servers.Clear();
                string jsonString = await System.IO.File.ReadAllTextAsync("Servers.json");
                var servers_list = await Task.Run(() =>
                {
                    return JsonSerializer.Deserialize<Servers[]>(jsonString);
                });
                if (servers_list != null)
                {
                    foreach (Servers server in servers_list)
                    {
                        Servers srv = server;
                        srv.Name = srv.Name.ToLower();
                        //servers.TryAdd(server.Name.ToLower(), server);
                        servers.Add(srv);
                    }
                }
            }
            catch (Exception ex)
            {
                //Logging
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Saves servers to file.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task SaveServers()
        {
            try
            {
                string jsonString = await Task.Run(() =>
                {
                    List<Servers> servers_list = [];
                    foreach (Servers server in servers)
                    {
                        /*server.Value.Name = server.Value.Name.ToLower();
                        servers_list.Add(server.Value);*/
                        servers_list.Add(server);
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
        /// <summary>
        /// Writes error log.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <returns>Async Task.</returns>
        public async Task WriteLog(Exception ex)
        {
            string log = DateTime.Now.ToString("d.M.yyyy. H:m:s") + " " + ex.ToString() + Environment.NewLine;
            try
            {
                await System.IO.File.AppendAllTextAsync("Client.log", log);
                foreach (PluginInfo plugininfo in plugins)
                {
                    try
                    {
                        await plugininfo.Plugin.ClientLog(ex);
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
                                await plugininfo.Plugin.WriteLog(ex1);
                            }
                            catch
                            {
                                //Disregard
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Can't save log to file.");
                // Console.WriteLine(log);
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
        private async Task UnpackPlugins()
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
                await WriteLog(ex);
            }
        }
        private async Task UnpackPatch(string path)
        {
            try
            {
                Directory.CreateDirectory("Patches");
                UnpackZip(path, Path.Combine("Patches", Path.GetFileNameWithoutExtension(path)));
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Loads patches.
        /// </summary>
        public async Task LoadPatch(string path)
        {
            await UnpackPatch(path);
            try
            {
                Directory.CreateDirectory("Patches");
                string[] pluginsnames = Directory.GetDirectories("Patches");
                foreach (string name in pluginsnames)
                {
                    try
                    {
                        if (Verify(Path.GetFullPath(name)))
                        {
                            string pluginname = Path.GetFileName(name);
                            string name1 = pluginname + ".dll";
                            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(name, name1)));
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
                                    plugininfo.Plugin.Client = this;
                                    try
                                    {
                                        if (plugininfo.Plugin.IsPatch)
                                        {
                                            harmony.PatchAll(plugininfo.Assembly);
                                            MessageBox.Show("Patched.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (ex is NotImplementedException)
                                        {
                                            //Disregard
                                        }
                                        else
                                        {
                                            await WriteLog(ex);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Patch signature error");
                        }
                    }
                    catch (Exception ex)
                    {
                        await WriteLog(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Loads plugins.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task LoadPlugins()
        {
            await UnpackPlugins();
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
                            if (!plugins.Exists(t => t.Name == pluginname))
                            {
                                string name1 = pluginname + ".dll";
                                Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(name, name1)));
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
                                        plugininfo.Plugin.Client = this;
                                        try
                                        {
                                            await plugininfo.Plugin.Initialize();
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
                                                    await plugininfo.Plugin.WriteLog(ex);
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
                                //Plugin is already loaded
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await WriteLog(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        /// <summary>
        /// Adds menue item to main menu.
        /// </summary>
        /// <param name="item"></param>
        public void AddMainMenu(ToolStripMenuItem item)
        {
            main.mainmenu.Items.Add(item);
            pluginmenuitems.Add(item);
        }
        /// <summary>
        /// Prepares Client for update.
        /// </summary>
        /// <param name="path">Path to update package.</param>
        public void PrepareUpdate(string path)
        {
            if (!string.IsNullOrEmpty(clientpath))
            {
                string updatepath = Path.Combine(clientpath, "Update");
                Directory.CreateDirectory(updatepath);
                UnpackZip(path, updatepath);
            }
        }
    }
}