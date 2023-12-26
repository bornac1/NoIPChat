using System.Xml.Serialization;

namespace Configuration
{
    [Serializable]
    public class Server
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
    public class Remote
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
        public required Server Server { get; set; }
        public Remote? Remote { get; set; }
    }
}
