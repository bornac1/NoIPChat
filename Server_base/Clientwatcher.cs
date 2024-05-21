﻿namespace Server_base
{
    public partial class Server
    {
        private static string? ParseNameVersion(string name)
        {
            //Format of name: 0.0.0 patch win-x64 or 0.0.0 win-x64
            string[] strings = name.Split(' ');
            return strings[0];
        }
        private static string? ParseNameRuntime(string name)
        {
            string[] strings = name.Split(' ');
            return strings[^1];
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
            try
            {
                if (e.FullPath != null && File.Exists(e.FullPath) && Path.GetExtension(e.FullPath).Equals(".nip", StringComparison.OrdinalIgnoreCase))
                {
                    string name = Path.GetFileName(e.FullPath);
                    string? version = ParseNameVersion(name);
                    string? runtime = ParseNameRuntime(name);
                    if (name.Contains("patch", StringComparison.OrdinalIgnoreCase) && version != null)
                    {
                        //It's a patch
                        if (runtime != null && clientpatches.TryGetValue(runtime, out var patches))
                        {
                            patches.Add((version, e.FullPath));
                        }
                        else if (runtime != null)
                        {
                            ConcurrentList<(string, string)> newpatches = [];
                            newpatches.Add((version, e.FullPath));
                            if (clientpatches.TryAdd(runtime, newpatches))
                            {
                                //Shouldn't fail
                            }
                        }
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
            try
            {
                string name = Path.GetFileName(e.FullPath);
                string? runtime = ParseNameRuntime(name);
                if (runtime != null && name.Contains("patch", StringComparison.OrdinalIgnoreCase) && clientpatches.TryGetValue(runtime, out var patches))
                {
                    foreach (var patch in patches)
                    {
                        if (e.FullPath == patch.Item2)
                        {
                            patches.Remove(patch);
                            if (patches.Count == 0)
                            {
                                if (!clientpatches.TryRemove(runtime, out _))
                                {
                                    //Already removed?
                                }
                            }
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
                string? runtime = ParseNameRuntime(name);
                if (runtime != null && name.Contains("patch", StringComparison.OrdinalIgnoreCase) && clientpatches.TryGetValue(runtime, out var patches))
                {
                    foreach (var patch in patches)
                    {
                        if (e.OldFullPath == patch.Item2)
                        {
                            patches.Remove(patch);
                            string? version = ParseNameVersion(name);
                            if (version != null)
                            {
                                patches.Add((version, e.FullPath));
                            }
                            break;
                        }
                    }
                }
                else
                {
                    clientupdatepath = e.FullPath;
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
    }
}