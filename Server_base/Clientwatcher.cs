namespace Server_base
{
    public partial class Server
    {
        private static string? ParseNameVersion(string name)
        {
            //Format of name: 0.0.0 patch win-x64 or 0.0.0 win-x64
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == ' ')
                {
                    return name[0..i];
                }
            }
            return null;
        }
        private static string? ParseNameRuntime(string name)
        {
            //TODO: optimize
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
                string? version;
                string? runtime;
                if (e.FullPath != null && File.Exists(e.FullPath) && Path.GetExtension(e.FullPath).Equals(".nip", StringComparison.OrdinalIgnoreCase))
                {
                    string name = Path.GetFileName(e.FullPath);
                    version = ParseNameVersion(name);
                    runtime = ParseNameRuntime(name);
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
                        name = Path.GetFileName(e.FullPath);
                        version = ParseNameVersion(name);
                        runtime = ParseNameRuntime(name);
                        if (version != null && runtime != null)
                        {
                            if (clientupdates.TryAdd(runtime, e.FullPath))
                            {
                                //Shouldn't fail
                            }
                            CVU = version;
                        }
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
                    name = Path.GetFileName(e.FullPath);
                    runtime = ParseNameRuntime(name);
                    if (runtime != null)
                    {
                        if (clientupdates.TryRemove(runtime, out _))
                        {
                            //Shouldn't fail
                        }
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
                    name = Path.GetFileName(e.FullPath);
                    runtime = ParseNameRuntime(name);
                    if (runtime != null)
                    {
                        if (clientupdates.TryGetValue(runtime, out string? current) && current != null)
                        {
                            if(clientupdates.TryUpdate(runtime, e.FullPath, current))
                            {
                                //Shouldn't fail
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
    }
}
