using System.Reflection;

namespace Client
{
    /// <summary>
    /// Used to store informations about plugins.
    /// </summary>
    public struct PluginInfo
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Assembly of the plugin.
        /// </summary>
        public required Assembly Assembly { get; set; }
        /// <summary>
        /// Plugin object.
        /// </summary>
        public required IPlugin Plugin { get; set; }
    }
}
