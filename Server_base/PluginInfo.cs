using System.Reflection;

namespace Server_base
{
    public struct PluginInfo
    {
        public required string Name { get; set; }
        public required Assembly Assembly { get; set; }
        public required IPlugin Plugin { get; set; }
    }
}
