using MessagePack;
namespace Messages
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class Message
    {
        /// <summary>
        /// CLient version.
        /// </summary>
        public int? CV { get; set; }
        public int? CVU { get; set; }
        /// <summary>
        /// Server version.
        /// </summary>
        public int? SV { get; set; }
        public int? SVU { get; set; }
        public bool? Update { get; set; }
        /// <summary>
        /// Username.
        /// </summary>
        public string? User { get; set; }
        /// <summary>
        /// List of users connected to the server
        /// </summary>
        public string? Users { get; set; }
        /// <summary>
        /// Password.
        /// </summary>
        public byte[]? Pass { get; set; }
        /// <summary>
        /// Server name.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Indicate that client is authenticated.
        /// </summary>
        public bool? Auth { get; set; }
        /// <summary>
        /// Indicate that client/server is to be disconnected.
        /// </summary>
        public bool? Disconnect { get; set; }
        /// <summary>
        /// Indicate that remote server is trying to connect.
        /// </summary>
        public bool? Server { get; set; }
        /// <summary>
        /// Sender username(s), if multiple, separated by ;
        /// </summary>
        public string? Sender { get; set; }
        /// <summary>
        /// Receiver username(s), if multiple, separated by ;
        /// </summary>
        public string? Receiver { get; set; }
        /// <summary>
        /// Text message to be sent.
        /// </summary>
        public byte[]? Msg { get; set; }
        /// <summary>
        /// Bynary data to be sent.
        /// </summary>
        public byte[]? Data { get; set; }
        public byte[]? Nounce { get; set; }
        public byte[]? PublicKey { get; set; }
        public int? Hop { get; set; }
    }
    [MessagePackObject(keyAsPropertyName: true)]
    public class ServerData
    {
        /// <summary>
        /// IP address.
        /// </summary>
        public string? IP { get; set; }
        /// <summary>
        /// Port.
        /// </summary>
        public int? Port { get; set; }
    }
    [MessagePackObject(keyAsPropertyName: true)]
    public class File
    {
        /// <summary>
        /// Name of the file.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Binary content of the file.
        /// </summary>
        public byte[]? Content { get; set; }
    }
    [MessagePackObject(keyAsPropertyName: true)]
    public class APIMessage
    {
        public string? Command { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool? Auth { get; set; }
        public byte[]? Message { get; set; }
    }
}