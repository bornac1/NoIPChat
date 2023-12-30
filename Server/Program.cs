using Messages;
using Sodium;
using System.Xml.Serialization;
namespace Server
{
    internal class Program
    {
        private static async Task StartServer(int attempt = 0)
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
                Configuration.Configuration? Config;
                using (TextReader reader = new StreamReader("Config.xml"))
                {
                    XmlSerializer serializer = new(typeof(Configuration.Configuration));
                    Config = (Configuration.Configuration?)serializer.Deserialize(reader);
                }
                if (Config != null)
                {
                    Server? server = null;
                    try
                    {
                        server = new(Config.Server.Name, Config.Server.Interfaces, ecdh);
                        while (true)
                        {
                            Console.ReadLine();
                        }
                    } catch (Exception ex)
                    {
                        //Leaked exceptions from server
                        Console.WriteLine("Server is closed.");
                        Console.WriteLine(ex.ToString());
                        if (server != null)
                        {
                            await server.Close();
                            server = null;
                            if(attempt <= 5)
                            {
                                Console.WriteLine("Trying to restart server");
                                await StartServer(attempt + 1);
                            }
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Error config.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error config.");
                Console.WriteLine(ex.ToString());
            }
        }
        static async Task Main()
        {
            await StartServer();
        }
    }
}