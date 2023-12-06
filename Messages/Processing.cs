using MessagePack;

namespace Messages
{
    public class Processing
    {
        /// <summary>
        /// Initializes processing.
        /// </summary>
        public Processing()
        {
        }
        /// <summary>
        /// Serialise given message into bytes.
        /// </summary>
        /// <param name="message">Message to be serialised.</param>
        public static async Task<byte[]?> Serialize(Message message)
        {
            try
            {
                byte[] bytes = await Task.Run(() => { return MessagePackSerializer.Serialize(message); });
                return bytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }
        /// <summary>
        /// Deseliazies bytes into message.
        /// </summary>
        /// <param name="bytes">Bytes to be deserialized.</param>
        public static async Task<Message> Deserialize(byte[] bytes)
        {
            try
            {
                Message message = await Task.Run(() => { return MessagePackSerializer.Deserialize<Message>(bytes); });
                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return new Message();
        }
        /// <summary>
        /// Serializes file into bytes.
        /// </summary>
        /// <param name="file">File to be serialized.</param>
        public static async Task<byte[]?> SerializeFile(File file)
        {
            byte[] bytes = await Task.Run(() => { return MessagePackSerializer.Serialize(file); });
            return bytes;
        }
        /// <summary>
        /// Deserializes bytes into file.
        /// </summary>
        /// <param name="bytes">Bytes to be deserialized.</param>
        public static async Task<File> DeserializeFile(byte[] bytes)
        {
            File file = await Task.Run(() => { return MessagePackSerializer.Deserialize<File>(bytes); });
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
        /// <param name="bytes">Bytes to be deserialized.</param>
        public static async Task<APIMessage> DeserializeAPI(byte[] bytes)
        {
            APIMessage message = await Task.Run(() => { return MessagePackSerializer.Deserialize<APIMessage>(bytes); });
            return message;
        }
    }
}
