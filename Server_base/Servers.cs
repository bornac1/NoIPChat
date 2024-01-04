using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server_base
{
    public class Servers
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Name { get; set; }
        public string LocalIP { get; set; }
        public string RemoteIP { get; set; }
        public int RemotePort { get; set; }
        public int TimeOut { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static string Serialize(List<Servers> servers)
        {
            return JsonSerializer.Serialize(servers, SourceGenerationContext.Default.ServersArray);
        }
        public static Servers[]? Deserialize(string servers)
        {
            return JsonSerializer.Deserialize(servers, SourceGenerationContext.Default.ServersArray);
        }
    }
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Servers))]
    [JsonSerializable(typeof(Servers[]))]
    [JsonSerializable(typeof(List<Servers>))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
