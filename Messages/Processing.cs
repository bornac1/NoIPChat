using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace Messages
{
    public class Processing
    {
        private MemoryStream stream;
        /// <summary>
        /// Initializes processing.
        /// </summary>
        public Processing()
        {
            stream = new MemoryStream();
        }
        /// <summary>
        /// Serialise given message into bytes.
        /// </summary>
        /// <param name="message">Message to be serialised.</param>
        public async Task<byte[]?> Serialize(Message message)
        {
            try
            {
                await MessagePackSerializer.SerializeAsync(stream, message);
                byte[] bytes = stream.ToArray();
                stream.Position = 0;
                stream.SetLength(0);
                return bytes;
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Deseliazies bytes into message.
        /// </summary>
        /// <param name="bytes">Bytes to be deserialized.</param>
        public async Task<Message> Deserialize(byte[] bytes)
        {
            try
            {
                await stream.WriteAsync(bytes);
                await stream.FlushAsync();
                stream.Position = 0;
                Message message = await MessagePackSerializer.DeserializeAsync<Message>(stream);
                stream.Position = 0;
                stream.SetLength(0);
                return message;
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new Message();
        }
        /// <summary>
        /// Deseliazies bytes from MemoryStream into message. It will delete data from MemoryStream.
        /// </summary>
        /// <param name="stream">MemoryStream</param>
        public async Task<Message> Deserialize(MemoryStream stream)
        {
            try
            {
                Message message = await MessagePackSerializer.DeserializeAsync<Message>(stream);
                stream.Position = 0;
                stream.SetLength(0);
                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new Message();
        }
        /// <summary>
        /// Serializes file into bytes.
        /// </summary>
        /// <param name="file">File to be serialized.</param>
        public async Task<byte[]> SerializeFile(File file)
        {
            await MessagePackSerializer.SerializeAsync(stream, file);
            byte[] bytes = stream.ToArray();
            stream.Position = 0;
            stream.SetLength(0);
            return bytes;
        }
        /// <summary>
        /// Deserializes bytes into file.
        /// </summary>
        /// <param name="bytes">Bytes to be deserialized.</param>
        public async Task<File> DeserializeFile(byte[] bytes)
        {
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
            stream.Position = 0;
            File file = await MessagePackSerializer.DeserializeAsync<File>(stream);
            stream.Position = 0;
            stream.SetLength(0);
            return file;
        }
        /// <summary>
        /// Closes processing. Should be called when it's no longer needed.
        /// </summary>
        public async Task Close()
        {
            stream.Close();
            await stream.DisposeAsync();
        }
        /// <summary>
        /// Serialise given APIMessage into bytes.
        /// </summary>
        /// <param name="message">Message to be serialised.</param>
        public async Task<byte[]> SerializeAPI(APIMessage message)
        {
            await MessagePackSerializer.SerializeAsync(stream, message);
            byte[] bytes = stream.ToArray();
            stream.Position = 0;
            stream.SetLength(0);
            return bytes;
        }
        /// <summary>
        /// Deseliazies bytes into APIMessage.
        /// </summary>
        /// <param name="bytes">Bytes to be deserialized.</param>
        public async Task<APIMessage> DeserializeAPI(byte[] bytes)
        {
            await stream.WriteAsync(bytes);
            await stream.FlushAsync();
            stream.Position = 0;
            APIMessage message = await MessagePackSerializer.DeserializeAsync<APIMessage>(stream);
            stream.Position = 0;
            stream.SetLength(0);
            return message;
        }
    }
}
