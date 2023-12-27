using Messages;
using Sodium;
using System.Xml.Serialization;
namespace Server
{
    internal class Program
    {
        private Server? server = null;
        private async Task StartServer()
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
                    try
                    {
                        server = new(Config.Server.Name, Config.Server.Interfaces, ecdh);
                    } catch (Exception ex)
                    {
                        //Leaked exceptions from server
                        if(server != null)
                        {
                            await server.Close();
                            server = null;
                        }
                        Console.WriteLine("Server is closed.");
                        Console.WriteLine(ex.ToString());
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
        private void StartShell()
        {
            if (server != null)
            {
                _ = new Shell(server);
            }
        }
        static void Main()
        {
            Program program = new();
            _ = program.StartServer();
            program.StartShell();
            //Don't close even if shell stops
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}