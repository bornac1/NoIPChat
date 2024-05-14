using System.Reflection;
using System.Text.Json;

namespace Server_base
{
    /// <summary>
    /// Used to store known servers.
    /// </summary>
    public class Servers
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Name of the known server.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Local IP of interface used to connect to known server.
        /// </summary>
        public string LocalIP { get; set; }
        /// <summary>
        /// IP of known server.
        /// </summary>
        public string RemoteIP { get; set; }
        /// <summary>
        /// Port on known server.
        /// </summary>
        public int RemotePort { get; set; }
        /// <summary>
        /// Timeout of connection in seconds.
        /// </summary>
        public int TimeOut { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Serializes Servers into JSON string.
        /// </summary>
        /// <param name="servers">Array of Servers.</param>
        /// <returns>JSON string.</returns>
        public static string Serialize(Servers[] servers)
        {
            return JsonSerializer.Serialize(servers);
        }
        /// <summary>
        /// Deserializes Servers from JSON string.
        /// </summary>
        /// <param name="servers">JSON string.</param>
        /// <returns>Array of Servers.</returns>
        public static Servers[]? Deserialize(string servers)
        {
            return JsonSerializer.Deserialize<Servers[]>(servers);
        }
        /// <summary>
        /// Run when Server_base needs to be unloaded.
        /// </summary>
        public static void Unloading()
        {
            var assembly = typeof(JsonSerializerOptions).Assembly;
            var updateHandlerType = assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler");
            var clearCacheMethod = updateHandlerType?.GetMethod("ClearCache", BindingFlags.Static | BindingFlags.Public);
            clearCacheMethod?.Invoke(null, [null]);
        }
    }
}
