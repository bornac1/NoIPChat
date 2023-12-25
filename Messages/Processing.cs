using MessagePack;

namespace Messages
{
    public static class Processing
    {
        /// <summary>
        /// Serialise given message into bytes.
        /// </summary>
        /// <param name="message">Message to be serialised.</param>
        public static async Task<byte[]> Serialize(Message message)
        {
            byte[] bytes = await Task.Run(() => { return MessagePackSerializer.Serialize(message); });
            return bytes;
        }
        /// <summary>
        /// Deserializes bytes into message.
        /// </summary>
        /// <param name="data">Binary data to be deserialized.</param>
        public static async Task<Message> Deserialize(ReadOnlyMemory<byte> data)
        {
            Message message = await Task.Run(() => { return MessagePackSerializer.Deserialize<Message>(data); });
            return message;
        }
        /// <summary>
        /// Serializes file into bytes.
        /// </summary>
        /// <param name="file">File to be serialized.</param>
        public static async Task<byte[]> SerializeFile(File file)
        {
            byte[] bytes = await Task.Run(() => { return MessagePackSerializer.Serialize(file); });
            return bytes;
        }
        /// <summary>
        /// Deserializes bytes into file.
        /// </summary>
        /// <param name="data">Binary data to be deserialized.</param>
        public static async Task<File> DeserializeFile(ReadOnlyMemory<byte> data)
        {
            File file = await Task.Run(() => { return MessagePackSerializer.Deserialize<File>(data); });
            return file;
        }
        /// <summary>
        /// Serialise given APIMessage into bytes.
        /// </summary>
        /// <param name="message">Message to be serialised.</param>
        public static async Task<byte[]> SerializeAPI(APIMessage message)
        {
            byte[] bytes = await Task.Run(() => { return MessagePackSerializer.Serialize(message); });
            return bytes;
        }
        /// <summary>
        /// Deseliazies bytes into APIMessage.
        /// </summary>
        /// <param name="data">Binary data to be deserialized.</param>
        public static async Task<APIMessage> DeserializeAPI(ReadOnlyMemory<byte> data)
        {
            APIMessage message = await Task.Run(() => { return MessagePackSerializer.Deserialize<APIMessage>(data); });
            return message;
        }
    }
}
