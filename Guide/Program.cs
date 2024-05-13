using System.Xml.Serialization;
using ConfigurationData;
namespace Guide
{
    internal class Program
    {
        static void Client()
        {
            Console.WriteLine("Client currently isn't supported.");
            Begin();
        }
        static void Server()
        {
            Console.WriteLine("What do you need to do with Server?");
            Console.Write("Type I for installation, C for Configuration or B to go back to main menu ");
        serverstart:
            string? s = Console.ReadLine();
            if (s != null)
            {
                if (s.Equals("I", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO: installation
                    Console.WriteLine("Installation currently isn't supported.");
                    Begin();

                }
                else if (s.Equals("C", StringComparison.OrdinalIgnoreCase))
                {
                    ServerConfig();
                    Begin();

                }
                else if (s.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    Begin();
                }
                else
                {
                    Console.WriteLine("Unknown. Try again.");
                    goto serverstart;
                }
            }
        }
        static void CreateServerConfig(string path)
        {
            Console.Write("Name of the server: ");
            string? name = Console.ReadLine();
            Console.Write("How many interfaces do you want to use?");
            List<Interface> interfaces = [];
            if (Int32.TryParse(Console.ReadLine(), out int n))
            {
                for (int i = 0; i < n; i++)
                {
                    Console.WriteLine($"Enter data for interface {i}.");
                    Console.Write("Interface IP address: ");
                    string? interfaceip = Console.ReadLine();
                    Console.Write("IP address: ");
                    string? ip = Console.ReadLine();
                    Console.Write("Port: ");
                    if (Int32.TryParse(Console.ReadLine(), out int port))
                    {
                        if (string.IsNullOrEmpty(ip))
                        {
                            ip = interfaceip;
                        }
                        if (!string.IsNullOrEmpty(interfaceip) && !string.IsNullOrEmpty(ip))
                        {
                            interfaces.Add(new Interface() { InterfaceIP = interfaceip, IP = ip, Port = port });
                            Console.WriteLine($"Interface {i} added successfully.");
                        }
                    }
                }
                string logfile = Path.Combine(path, "Server.log");
                Console.WriteLine("Log will be saved to " + logfile);
                Console.Write("Custom log path: (leave empty if you want default one) ");
                string? logfile1 = Console.ReadLine();
                if (!string.IsNullOrEmpty(logfile1))
                {
                    logfile = logfile1;
                    Console.WriteLine($"New log file path is {logfile}");
                }
                //TODO: Remote
                if (name != null)
                {
                    Configuration config = new()
                    {
                        Server = new ServerConfiguration() { Name = name, Interfaces = interfaces },
                        Logfile = logfile
                    };
                    using TextWriter writer = new StreamWriter(Path.Combine(path, "Config.xml"));
                    XmlSerializer serializer = new(typeof(Configuration));
                    serializer.Serialize(writer, config);
                }
            }
        }
        static void ServerConfig()
        {
        serverconfigstart:
            Console.WriteLine("Welcome to Server configurator for NoIPChat.");
            string path = Directory.GetCurrentDirectory();
        checkpath:
            Console.WriteLine("Path: " + path);
            Console.Write("Is the path above pointing to the NoIPChat Server folder? Type Y for yes or N for no. ");
        input:
            string? c = Console.ReadLine();
            if (c != null)
            {
                if (c.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    //Correct path, continue
                    if (File.Exists(Path.Combine(path, "Config.xml")))
                    {
                        Console.WriteLine("Configuration file already exists.");
                        Console.WriteLine("Old config file will be renamed to Config.xml.backup.");
                        File.Move(Path.Combine(path, "Config.xml"), Path.Combine(path, "Config.xml.backup"));
                        CreateServerConfig(path);
                    }
                    else
                    {
                        Console.WriteLine("Configuration file doesn't exist and new one will be created.");
                        CreateServerConfig(path);
                    }
                }
                else if (c.Equals("N", StringComparison.OrdinalIgnoreCase))
                {
                    //Wrong path, ask for path
                    Console.Write("Please provide the correct path to NoIPChat Server folder. ");
                    string? path1 = Console.ReadLine();
                    if (path1 != null)
                    {
                        path = path1;
                        goto checkpath;
                    }
                    else
                    {
                        Console.Write("Type B to go back to Server menue.");
                        goto input;
                    }
                }
                else if (c.Equals("B", StringComparison.OrdinalIgnoreCase))
                {
                    Server();
                }
                else
                {
                    Console.WriteLine("Unknown. Try again.");
                    goto serverconfigstart;
                }
            }
        }
        static void Begin()
        {
        beginstart:
            Console.WriteLine("Welcome to NoIPChat.");
            Console.WriteLine("Do you need help with Client or Server?");
            Console.Write("Type C for Client and S for Server ");
            string? type = Console.ReadLine();
            if (type != null)
            {
                if (type.Equals("C", StringComparison.OrdinalIgnoreCase))
                {
                    Client();
                }
                else if (type.Equals("S", StringComparison.OrdinalIgnoreCase))
                {
                    Server();
                }
                else
                {
                    Console.WriteLine("Unkown type.");
                    goto beginstart;
                }
            }
        }
        static void Main()
        {
            Begin();
        }
    }
}
