using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Server_base
{
    public partial class Server
    {
        private static string? ParseName(string name)
        {
            //Format of name: 0.0.0 patch
            for(int i =0; i< name.Length; i++)
            {
                if (name[i] == ' ')
                {
                    return name[0..i];
                }
            }
            return null;
        }
        private void Setupclientwatcher()
        {
            Directory.CreateDirectory("Clientupdates");
            clientwatcher.Path = Path.GetFullPath("Clientupdates");
            clientwatcher.Created += ClientwatcherOncreated;
            //clientwatcher.Changed += ClientwtacherOnchanged;
            clientwatcher.Deleted += ClientwatcherOndeleted;
            clientwatcher.Renamed += ClientwatcherOnrenamed;
            clientwatcher.Error += ClientwatcherOnerror;
            clientwatcher.EnableRaisingEvents = true;
        }
        private async void ClientwatcherOnerror(object sender, ErrorEventArgs e)
        {
            await WriteLog(e.GetException());
        }
        private async void ClientwatcherOncreated(object sender, FileSystemEventArgs e)
        {
            try {
                if (e.FullPath != null && File.Exists(e.FullPath) && Path.GetExtension(e.FullPath).Equals(".nip", StringComparison.OrdinalIgnoreCase))
                {
                    string name = Path.GetFileName(e.FullPath);
                    string? version = ParseName(name);
                    if (name.Contains("patch", StringComparison.OrdinalIgnoreCase) && version != null)
                    {
                        //It's a patch
                        clientpatches.Add((version, e.FullPath));
                    }
                    else
                    {
                        //It's update
                        clientupdatepath = e.FullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        private async void ClientwatcherOndeleted(object sender, FileSystemEventArgs e)
        {
            try {
                string name = Path.GetFileName(e.FullPath);
                if (name.Contains("patch", StringComparison.OrdinalIgnoreCase)){
                    foreach (var patch in clientpatches)
                    {
                        if (e.FullPath == patch.Item2)
                        {
                            clientpatches.Remove(patch);
                            break;
                        }
                    }
                }
                else
                {
                    if (clientupdatepath == e.FullPath)
                    {
                        clientupdatepath = null;
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        private async void ClientwatcherOnrenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                string name = Path.GetFileName(e.FullPath);
                if (name.Contains("patch", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var patch in clientpatches)
                    {
                        if (e.OldFullPath == patch.Item2)
                        {
                            clientpatches.Remove(patch);
                            string? version = ParseName(name);
                            if (version != null)
                            {
                                clientpatches.Add((version, e.FullPath));
                            }
                            break;
                        }
                    }
                } else {
                    clientupdatepath = e.FullPath;
                }
            } catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
    }
}
