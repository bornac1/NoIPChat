namespace Server
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
    }
}
