using MessagePack;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        /// Serializes given APIMessage into bytes.
        /// </summary>
        /// <param name="message">Message to be serialized.</param>
        public static async Task<byte[]> SerializeAPI(APIMessage message)
        {
            byte[] bytes = await Task.Run(() => { return MessagePackSerializer.Serialize(message); });
            return bytes;
        }
        /// <summary>
        /// Deserializes bytes into APIMessage.
        /// </summary>
        /// <param name="data">Binary data to be deserialized.</param>
        public static async Task<APIMessage> DeserializeAPI(ReadOnlyMemory<byte> data)
        {
            APIMessage message = await Task.Run(() => { return MessagePackSerializer.Deserialize<APIMessage>(data); });
            return message;
        }
        /// <summary>
        /// Serilizes generic type.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="value">Value to be serialized.</param>
        /// <returns>Async Task that completes with byte[].</returns>
        public static async Task<byte[]> SerializeGeneric<T>(T value)
        {
            return await Task.Run(() => { return MessagePackSerializer.Serialize(value); });
        }
        /// <summary>
        /// Deserialize generic type.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="data">byte[] to be deserialized.</param>
        /// <returns>Async Task that complets with object of type T.</returns>
        public static async Task<T> DeserializeGeneric<T>(byte[] data)
        {
            return await Task.Run(() => { return MessagePackSerializer.Deserialize<T>(data); });
        }
    }
}
