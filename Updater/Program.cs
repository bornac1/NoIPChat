using System.Diagnostics;

namespace Updater
{
    internal class Program
    {
        static void Main(string[] args)
        {
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
                            }
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
                        else
                        {

                        }

                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
