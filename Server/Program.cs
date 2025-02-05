﻿using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml.Serialization;
using ConfigurationData;
using Messages;
using Server_interface;
using Sodium;
namespace Server
{
    internal class Program
    {
        private IServer? server;
        private IRemote? remote;
        private WriteLogAsync? writelogasync;

        private AssemblyLoadContext? context;
        private WeakReference? contextref;
        Type? Server_class;
        Type? Remote_class;
        private Program()
        {
            server = null;
            remote = null;
            writelogasync = null;
            context = null;
            contextref = null;
            Server_class = null;
            Remote_class = null;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Load(string name)
        {
            try
            {
                context = new(null, true);
                contextref = new(context);
                string? path = Path.GetDirectoryName(AppContext.BaseDirectory);
                if (path != null)
                {
                    Assembly? loaded = context.LoadFromAssemblyPath(Path.Combine(path, name));
                    Server_class = loaded?.GetTypes().Where(t => typeof(IServer).IsAssignableFrom(t) && !t.IsInterface).FirstOrDefault();
                    Remote_class = loaded?.GetTypes().Where(t => typeof(IRemote).IsAssignableFrom(t) && !t.IsInterface).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task Unload()
        {
            try
            {
                writelogasync = null;
                Server_class = null;
                Remote_class = null;
                if (server != null)
                {
                    await server.Close();
                    if (await server.Closed.Task)
                    {
                        Console.WriteLine("Server closed");
                    }
                    server = null;
                }
                if (remote != null)
                {
                    remote.Close();
                    remote = null;
                }
                context?.Unload();
                context = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Clean()
        {
            if (contextref != null)
            {
                for (int i = 0; contextref.IsAlive && i < 10; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                Console.WriteLine($"Unload success: {!contextref?.IsAlive}");
            }
        }
        private IServer CreateServer(string name, List<Interface> interfaces, KeyPair ecdh, WriteLogAsync? writelogasync, string? logfile)
        {
            if (Server_class != null)
            {
                var srv = Activator.CreateInstance(Server_class, name, interfaces, ecdh, writelogasync, logfile, context);
                if (srv != null)
                {
                    return (IServer)srv;
                }
            }
            throw new NullReferenceException();
        }
        private IRemote CreateRemote(string IP, int port, string username, string password)
        {
            if (Remote_class != null)
            {
                var rem = Activator.CreateInstance(Remote_class, IP, port, username, password);
                if (rem != null)
                {
                    return (IRemote)rem;
                }
            }
            throw new NullReferenceException();
        }
        private async Task StartServer(int attempt = 0)
        {
            KeyPair ecdh;
            try
            {
                byte[] key = System.IO.File.ReadAllBytes("Key.bin");
                if (key.Length == 32)
                {
                    ecdh = Encryption.GetECDH(key);
                }
                else
                {
                    {
                        throw new Exception();
                    }
                }
            }
            catch
            {
                //There was an error with reading a file
                //Generate new key
                Console.WriteLine("ECDH key error. New one is genearted.");
                ecdh = Encryption.GenerateECDH();
                try
                {
                    System.IO.File.WriteAllBytes("Key.bin", ecdh.PrivateKey);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Can't save ECDH key.");
                    Console.WriteLine(ex.ToString());
                }
            }
            try
            {
                Configuration? Config;
                using (TextReader reader = new StreamReader("Config.xml"))
                {
                    XmlSerializer serializer = new(typeof(Configuration));
                    Config = (Configuration?)serializer.Deserialize(reader);
                }
                if (Config != null)
                {
                    try
                    {
                        server = CreateServer(Config.Server.Name, Config.Server.Interfaces, ecdh, writelogasync, Config.Logfile);
                    }
                    catch (Exception ex)
                    {
                        //Leaked exceptions from server
                        Console.WriteLine("Server is closed.");
                        string message = ex.ToString();
                        Console.WriteLine(message);
                        if (remote != null)
                        {
                            await remote.SendLog(message);
                        }
                        if (server != null)
                        {
                            await server.Close();
                            server = null;
                            if (attempt <= 5)
                            {
                                Console.WriteLine("Trying to restart server");
                                await StartServer(attempt + 1);
                            }
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error config server no exception.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error config server.");
                Console.WriteLine(ex.ToString());
            }
        }
        private void StartRemote(int attempt = 0)
        {
            try
            {
                Configuration? Config;
                using (TextReader reader = new StreamReader("Config.xml"))
                {
                    XmlSerializer serializer = new(typeof(Configuration));
                    Config = (Configuration?)serializer.Deserialize(reader);
                }
                if (Config != null && Config.Remote != null)
                {
                    try
                    {
                        if (Config.Remote.Active)
                        {
                            remote = CreateRemote(Config.Remote.IP, Config.Remote.Port, Config.Remote.User, Config.Remote.Pass);
                            writelogasync = remote.SendLog;
                            if (server != null)
                            {
                                server.Writelogasync = writelogasync;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //Leaked exceptions from Remote
                        Console.WriteLine("Remote is closed.");
                        Console.WriteLine(ex.ToString());
                        if (remote != null)
                        {
                            remote.Close();
                            remote = null;
                            writelogasync = null;
                            if (server != null)
                            {
                                server.Writelogasync = writelogasync;
                            }
                            if (attempt <= 5)
                            {
                                Console.WriteLine("Trying to restart remote");
                                StartRemote(attempt + 1);
                            }
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error config remote no exception.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error config remote.");
                Console.WriteLine(ex.ToString());
            }
        }
        private static void UnpackZip(string zipFilePath, string extractPath)
        {
            Directory.CreateDirectory(extractPath);
            using ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName != "sign")
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
        }
        static void Update()
        {
            try
            {
                string? serverpath = Path.GetDirectoryName(AppContext.BaseDirectory);
                Console.Write("Path to update pack: ");
                string? path = Console.ReadLine();
                if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(serverpath))
                {
                    string updatepath = Path.Combine(serverpath, "Update");
                    Directory.CreateDirectory(updatepath);
                    UnpackZip(path, updatepath);
                    string[] files = Directory.GetFiles(updatepath);
                    if (files.Length > 0)
                    {
                        if (Directory.Exists("Backup"))
                        {
                            Directory.Delete("Backup", true);
                        }
                        Directory.CreateDirectory("Backup");
                        if (Directory.Exists("Patches"))
                        {
                            string backuppatch = Path.Combine("Backup", "Patches");
                            Directory.CreateDirectory(backuppatch);
                            foreach (string pfile in Directory.GetFiles("Patches"))
                            {
                                System.IO.File.Copy(pfile, Path.Combine(backuppatch, Path.GetFileName(pfile)));
                            }
                            Directory.Delete("Patches", true);
                        }
                        foreach (string file in files)
                        {
                            string file1 = Path.GetFullPath(file);
                            string filename = Path.GetFileName(file);
                            string oldpath = Path.Combine(serverpath, filename);
                            try
                            {
                                if (System.IO.File.Exists(oldpath))
                                {
                                    //Copy old to Backup
                                    System.IO.File.Copy(oldpath, Path.Combine("Backup", filename), true);
                                }
                                //Move new to old
                                //Deletes file from Update folder
                                System.IO.File.Move(file1, oldpath, true);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Update error. {ex}");
                                Console.WriteLine("Update can't be done without restarting. Type command update-force and Server will restart.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Path error.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        static void UpdateForce()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "Updater.exe",
                    Arguments = Path.GetDirectoryName(AppContext.BaseDirectory) + " " + "server"
                });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void Patch()
        {
            if (server != null)
            {
                Console.Write("Path to patch pack: ");
                string? path = Console.ReadLine();
                if (path != null)
                {
                    server.LoadPatch(path);
                    Console.WriteLine("Patched.");
                }
            }
        }
        private async Task SaveSneakernet()
        {
            Console.Write("Path for saving messages:");
            string? path = Console.ReadLine();
            Console.Write("Server for which messages are saved:");
            string? srv = Console.ReadLine();
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(srv) && server != null)
            {
                path = Path.GetFullPath(path);
                await server.SaveSneakernet(path, srv.ToLower());
            }
        }
        private async Task LoadSneakernet()
        {
            Console.Write("Path to saved messages:");
            string? path = Console.ReadLine();
            if (!string.IsNullOrEmpty(path) && server != null)
            {
                path = Path.GetFullPath(path);
                await server.LoadSneakernet(path);
            }
        }
        static async Task Main()
        {
            Console.Clear();
            Program program = new();
            program.Load("Server_base.dll");
            program.StartRemote();
            await program.StartServer();
            while (true)
            {
                string? input = Console.ReadLine();
                if (input != null)
                {
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        program.Unload().Wait();
                        break;
                    }
                    else if (input.Equals("update", StringComparison.OrdinalIgnoreCase))
                    {
                        program.Unload().Wait();
                        program.Clean();
                        if (program.contextref != null && !program.contextref.IsAlive)
                        {
                            Update();
                            program.Load("Server_base.dll");
                            program.StartRemote();
                            await program.StartServer();
                        }
                    }
                    else if (input.Equals("update-force", StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateForce();
                    }
                    else if (input.Equals("patch", StringComparison.OrdinalIgnoreCase))
                    {
                        program.Patch();
                    }
                    else if (input.Equals("save sneakernet", StringComparison.OrdinalIgnoreCase))
                    {
                        await program.SaveSneakernet();
                    }
                    else if (input.Equals("load sneakernet", StringComparison.OrdinalIgnoreCase))
                    {
                        await program.LoadSneakernet();
                    }
                    else if (input.Equals("start discovery", StringComparison.OrdinalIgnoreCase))
                    {
                        if (program.server != null)
                        {
                            await program.server.StartDiscovery();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unknown command.");
                    }
                }
            }
        }
    }
}