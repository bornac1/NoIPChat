using Messages;
using Sodium;
using System.Xml.Serialization;
namespace Server
{
    internal class Program
    {
        static void Main()
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
                catch
                {
                    Console.WriteLine("Can't save ECDH key.");
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
                    Server server = new(Config.Server.Name, Config.Server.Interfaces, ecdh);
                }
                else
                {
                    Console.WriteLine("Error config.");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error config.");
            }
            Console.ReadLine();
        }
    }
}