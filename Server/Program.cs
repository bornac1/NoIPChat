using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml.Serialization;
using ConfigurationData;
using Messages;
using Server_interface;
using Sodium;
namespace Server_starter
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
                string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (path != null)
                {
                    Assembly? loaded = context.LoadFromAssemblyPath(Path.Combine(path, name));
                    Server_class = loaded?.GetTypes().Where(t => typeof(IServer).IsAssignableFrom(t) && !t.IsInterface).FirstOrDefault();
                    Remote_class = loaded?.GetTypes().Where(t => typeof(IRemote).IsAssignableFrom(t) && !t.IsInterface).FirstOrDefault();
                }
            } catch (Exception ex)
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
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Clean()
        {
            if (contextref != null)
            {
                for (int i = 0; contextref.IsAlive && (i < 10); i++)
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
        private void Update()
        {
            Console.Write("Path to update folder: ");
            string? path = Console.ReadLine();
            if(path != null)
            {
                string? serverpath = Assembly.GetExecutingAssembly().Location;
                string[] files = Directory.GetFiles(path);
                foreach(string file in files) {
                    Console.WriteLine($"update paths {file} {serverpath}");
                }
            }
            else
            {
                Console.WriteLine("Path error.");
            }
        }
        static async Task Main()
        {
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
                        program.Update();
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