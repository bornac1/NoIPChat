using System.Xml.Serialization;

namespace ConfigurationData
{
    [Serializable]
    public class ServerConfiguration
    {
        public required string Name { get; set; }
        public required List<Interface> Interfaces { get; set; }
    }

    [Serializable]
    public class Interface
    {
        public required string InterfaceIP { get; set; }
        public required string IP { get; set; }
        public required int Port { get; set; }
    }

    [Serializable]
    public class RemoteConfiguration
    {
        public required bool Active { get; set; }
        public required string IP { get; set; }
        public required int Port { get; set; }
        public required string User { get; set; }
        public required string Pass { get; set; }
    }

    [Serializable]
    [XmlRoot("Configuration")]
    public class Configuration
    {
        public required ServerConfiguration Server { get; set; }
        public RemoteConfiguration? Remote { get; set; }
        public string? Logfile { get; set; }
    }
}
