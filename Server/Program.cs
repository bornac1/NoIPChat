using ConfigurationData;
using Messages;
using Server_base;
using Sodium;
using System.Xml.Serialization;
namespace Server_starter
{
    internal class Program
    {
        private Server? server = null;
        private Remote? remote = null;
        private Server.WriteLogAsync? writelogasync;
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
                        server = new(Config.Server.Name, Config.Server.Interfaces, ecdh, writelogasync);
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
                            remote = new Remote(Config.Remote.IP, Config.Remote.Port, Config.Remote.User, Config.Remote.Pass);
                            writelogasync = remote.SendLog;
                            if (server != null)
                            {
                                server.writelogasync = writelogasync;
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
        static async Task Main()
        {
            Program program = new();
            program.StartRemote();
            await program.StartServer();
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}