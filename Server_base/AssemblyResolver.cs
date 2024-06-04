using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Server_base
{
    public partial class Server
    {
        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            foreach (string path in Directory.GetDirectories("Plugins"))
            {
                if (path != null)
                {
                    string file = Path.GetFullPath(Path.Combine(path, $"{assemblyName.Name}.dll"));
                    if (System.IO.File.Exists(file))
                    {
                        return context.LoadFromAssemblyPath(file);
                    }
                }
            }
            throw new FileNotFoundException($"{assemblyName.Name}.dll not found");
        }
    }
}
