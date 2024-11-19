namespace Server_base
{
    public partial class Server
    {
        private void Setupserverwatcher()
        {
            Directory.CreateDirectory("Serverupdates");
            serverwatcher.Path = Path.GetFullPath("Serverupdates");
            serverwatcher.Created += ServerwatcherOncreated;
            //serverwatcher.Changed += ServerwtacherOnchanged;
            serverwatcher.Deleted += ServerwatcherOndeleted;
            serverwatcher.Renamed += ServerwatcherOnrenamed;
            serverwatcher.Error += ServerwatcherOnerror;
            serverwatcher.EnableRaisingEvents = true;
        }
        private async void ServerwatcherOnerror(object sender, ErrorEventArgs e)
        {
            await WriteLog(e.GetException());
        }
        private async void ServerwatcherOncreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                string? version;
                string? runtime;
                if (e.FullPath != null && File.Exists(e.FullPath) && Path.GetExtension(e.FullPath).Equals(".nip", StringComparison.OrdinalIgnoreCase))
                {
                    string name = Path.GetFileNameWithoutExtension(e.FullPath);
                    version = ParseNameVersion(name);
                    runtime = ParseNameRuntime(name);
                    if (name.Contains("patch", StringComparison.OrdinalIgnoreCase) && version != null)
                    {
                        //It's a patch
                        if (runtime != null && serverpatches.TryGetValue(runtime, out var patches))
                        {
                            patches.Add((version, e.FullPath));
                        }
                        else if (runtime != null)
                        {
                            ConcurrentList<(string, string)> newpatches = [];
                            newpatches.Add((version, e.FullPath));
                            if (serverpatches.TryAdd(runtime, newpatches))
                            {
                                //Shouldn't fail
                            }
                        }
                    }
                    else
                    {
                        //It's update
                        name = Path.GetFileNameWithoutExtension(e.FullPath);
                        version = ParseNameVersion(name);
                        runtime = ParseNameRuntime(name);
                        if (version != null && runtime != null)
                        {
                            if (serverupdates.TryAdd(runtime, e.FullPath))
                            {
                                //Shouldn't fail
                            }
                            SVU = version;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await WriteLog(ex);
            }
        }
        private async void ServerwatcherOndeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(e.FullPath);
                string? runtime = ParseNameRuntime(name);
                if (runtime != null && name.Contains("patch", StringComparison.OrdinalIgnoreCase) && serverpatches.TryGetValue(runtime, out var patches))
                {
                    foreach (var patch in patches)
                    {
                        if (e.FullPath == patch.Item2)
                        {
                            patches.Remove(patch);
                            if (patches.Count == 0)
                            {
                                if (!serverpatches.TryRemove(runtime, out _))
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
                    name = Path.GetFileNameWithoutExtension(e.FullPath);
                    runtime = ParseNameRuntime(name);
                    if (runtime != null)
                    {
                        if (serverupdates.TryRemove(runtime, out _))
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
        private async void ServerwatcherOnrenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(e.FullPath);
                string? runtime = ParseNameRuntime(name);
                if (runtime != null && name.Contains("patch", StringComparison.OrdinalIgnoreCase) && serverpatches.TryGetValue(runtime, out var patches))
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
                    name = Path.GetFileNameWithoutExtension(e.FullPath);
                    runtime = ParseNameRuntime(name);
                    if (runtime != null)
                    {
                        if (serverupdates.TryGetValue(runtime, out string? current) && current != null)
                        {
                            if (serverupdates.TryUpdate(runtime, e.FullPath, current))
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
