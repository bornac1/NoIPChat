using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Server_base
{
    public struct PluginInfo
    {
        public required string Name { get; set; }
        public required Assembly Assembly { get; set; }
        public required IPlugin Plugin { get; set; }
    }
}
