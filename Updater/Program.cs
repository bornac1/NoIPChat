using System.Diagnostics;

namespace Updater
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool err = false;
            Console.Clear();
            try
            {
                if (args.Length > 1)
                {
                    //First parameter is path
                    if (!string.IsNullOrEmpty(args[0]) && !string.IsNullOrEmpty(args[1]))
                    {
                        string path = args[0];
                        string type = args[1];
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
                        foreach (string file in Directory.GetFiles(Path.Combine(path, "Update")))
                        {
                            string file1 = Path.GetFullPath(file);
                            string filename = Path.GetFileName(file);
                            string oldpath = Path.Combine(path, filename);
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
                                err = true;
                            }
                        }
                        if (err)
                        {
                            Console.WriteLine("Not all files were replaced! Check log.");
                            Console.ReadLine();
                        }
                        if (type == "server")
                        {
                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            {
                                Process.Start(Path.Combine(path, "Server.exe"));
                                Environment.Exit(0);
                            }
                            else if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                Process.Start(Path.Combine(path, "Server"));
                                Environment.Exit(0);
                            }
                            else
                            {
                                Console.WriteLine("Server can't be restarted automatically. Please start it manually.");
                                Console.Write("Press any key to exit.");
                                Console.ReadLine();
                            }
                        }
                        else if (type == "client")
                        {
                            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            {
                                Process.Start(Path.Combine(path, "Client.exe"));
                                Environment.Exit(0);
                            }
                            else if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                Process.Start(Path.Combine(path, "Client"));
                                Environment.Exit(0);
                            }
                            else
                            {
                                Console.WriteLine("Client can't be restarted automatically. Please start it manually.");
                                Console.Write("Press any key to exit.");
                                Console.ReadLine();
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
