using Configuration;
using System;
using System.IO;
using System.Xml.Serialization;
namespace Server
{
    internal class Program
    {
        static void Main()
        {
            try
            {
                Configuration.Configuration? Config;
                using (TextReader reader = new StreamReader("Config.xml"))
                {
<<<<<<< Updated upstream
                    XmlSerializer serializer = new XmlSerializer(typeof(Configuration.Configuration));
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                Config = (Configuration.Configuration)serializer.Deserialize(reader);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
=======
                    XmlSerializer serializer = new(typeof(Configuration.Configuration));
                    Config = (Configuration.Configuration?)serializer.Deserialize(reader);
                }
                if (Config != null)
                {
                    Server server = new(Config.Server.Name, Config.Server.IP, Config.Server.Port);
                }
                else
                {
                    Console.WriteLine("Error config.");
                }
>>>>>>> Stashed changes
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Server server = new Server(Config.Server.Name, Config.Server.IP, Config.Server.Port);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            } catch (Exception ex)
            {
                Console.WriteLine("Error config.");
            }
            Console.ReadLine();
        }
    }
}