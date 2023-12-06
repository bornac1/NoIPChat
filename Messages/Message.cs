using MessagePack;
namespace Messages
{
    [MessagePackObject(keyAsPropertyName: true)]
    public struct Message
    {
        /// <summary>
        /// CLient version.
        /// </summary>
        public float? CV { get; set; }
        public float? CVU { get; set; }
        /// <summary>
        /// Server version.
        /// </summary>
        public float? SV { get; set; }
        public float? SVU { get; set; }
        public bool? Update {  get; set; }
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
        public string? Pass { get; set; }
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
        public string? Msg { get; set; }
        /// <summary>
        /// Bynary data to be sent.
        /// </summary>
        public byte[]? Data { get; set; }
    }
    [MessagePackObject(keyAsPropertyName: true)]
    public struct ServerData
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
    public struct File
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
    public struct APIMessage
    {
        public string? Command { get; set; }
        public byte[]? Message { get; set; }
    }
}