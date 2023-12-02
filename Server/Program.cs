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
            } catch (Exception ex)
            {
                Console.WriteLine("Error config.");
            }
            Console.ReadLine();
        }
    }
}