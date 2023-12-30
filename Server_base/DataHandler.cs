using Messages;
using System.Buffers.Binary;
using System.Text;

namespace Server
{
    public class DataHandler
    {
        private const string magicstring = "NOIPCHAT";
        private readonly int magiclength = Encoding.ASCII.GetBytes(magicstring).Length;
        private const int magicdata = 0;
        private const int magicsneakernet = 1;

        private readonly FileStream file;
        private readonly byte[] intbuffer = new byte[sizeof(int)];
        private byte[] messagebuffer = new byte[1024];
        private int version;
        private readonly string path;
        private long start = 0;
        private DataHandler(string folder, string name, bool temp = false)
        {
            name = string.Join('.', name, "noicb");
            path = Path.Combine(folder, name);
            Directory.CreateDirectory(folder);
            if (temp)
            {
                file = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.WriteThrough | FileOptions.DeleteOnClose);
            }
            else
            {
                file = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.WriteThrough);
            }
        }
        private DataHandler(string path, bool temp = false)
        {
            this.path = path;
            if (temp)
            {
                file = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.WriteThrough | FileOptions.DeleteOnClose);
            }
            else
            {
                file = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 0, FileOptions.Asynchronous | FileOptions.WriteThrough);
            }
        }
        /// <summary>
        /// Creates DataHandler for Data file.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="version">Server version.</param>
        /// <returns>Task that completes with DataHandler.</returns>
        /// <exception cref="VersionException">File version is newer than server.</exception>
        /// <exception cref="FileException">File error.</exception>
        public static async Task<DataHandler> CreateData(string name, int version)
        {
            DataHandler handler = new("Data", name);
            if (handler.file.Length >= sizeof(int))
            {
                await handler.ReadHeader();
                if (handler.version > version)
                {
                    throw new VersionException("File version is newer than server.");
                }
            }
            else
            {
                handler.version = version;
                //Write header
                await handler.WriteHeader();
                await handler.WriteInt(magicdata);
            }
            return handler;
        }
        /// <summary>
        /// Creates DataHandler for Temp file.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="version">Server version.</param>
        /// <returns>Task that completes with DataHandler.</returns>
        /// <exception cref="VersionException">File version is newer than server.</exception>
        /// <exception cref="FileException">File error.</exception>
        public static async Task<DataHandler> CreateTemp(string name, int version)
        {
            DataHandler handler = new("Temp", name, true);
            if (handler.file.Length >= sizeof(int))
            {
                await handler.ReadHeader();
                if (handler.version > version)
                {
                    throw new VersionException("File version is newer than server.");
                }
            }
            else
            {
                handler.version = version;
                //Write Header
                await handler.WriteHeader();
            }
            return handler;
        }
        /// <summary>
        /// Creates DataHandler for Sneakernet file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="version">Server version.</param>
        /// <returns>Task that completes with DataHandler.</returns>
        /// <exception cref="VersionException">File version is newer than server.</exception>
        /// <exception cref="FileException">File error.</exception>
        public static async Task<DataHandler> CreateSneakernet(string path, int version)
        {
            DataHandler handler = new(path);
            if (handler.file.Length >= sizeof(int))
            {
                await handler.ReadHeader();
                if (handler.version > version)
                {
                    throw new VersionException("File version is newer than server.");
                }
            }
            else
            {
                handler.version = version;
                //Write Header
                await handler.WriteHeader();
                await handler.WriteInt(magicsneakernet);
            }
            return handler;
        }
        private async Task WriteHeader()
        {
            byte[] data = Encoding.ASCII.GetBytes(magicstring);
            await file.WriteAsync(data);
            await WriteInt(version);
            start = file.Position;
        }
        private async Task ReadHeader()
        {
            byte[] buffer = new byte[magiclength];
            int read = 0;
            while (read < buffer.Length)
            {
                read += await file.ReadAsync(buffer.AsMemory(read, buffer.Length - read));
            }
            if (MemoryExtensions.Equals(Encoding.ASCII.GetString(buffer, 0, buffer.Length), magicstring, StringComparison.OrdinalIgnoreCase))
            {
                version = await ReadInt();
                await ReadInt();//Reads magic
            }
            else
            {
                throw new FileException("File error.");
            }
        }
        private async Task<int> ReadInt()
        {
            int read = 0;
            while (read < intbuffer.Length)
            {
                read += await file.ReadAsync(intbuffer.AsMemory(read, intbuffer.Length - read));
            }
            return BinaryPrimitives.ReadInt32LittleEndian(intbuffer.AsSpan());
        }
        private async Task WriteInt(int data)
        {
            BinaryPrimitives.WriteInt32LittleEndian(intbuffer.AsSpan(), data);
            await file.WriteAsync(intbuffer);
        }
        private void Handlemessagebuffer(int size)
        {
            if (messagebuffer.Length >= size)
            {
                //Buffer is large enough
                if (messagebuffer.Length / size >= 2)
                {
                    //Buffer is at least 2 times too large
                    int ns = messagebuffer.Length / (messagebuffer.Length / size);
                    messagebuffer = new byte[ns];
                }
            }
            else
            {
                int ns = size / messagebuffer.Length;
                if (size % messagebuffer.Length != 0)
                {
                    ns += 1;
                }
                ns *= messagebuffer.Length;
                messagebuffer = new byte[ns];
            }
        }
        private async Task<Message> ReadMessage()
        {
            int length = await ReadInt();
            Handlemessagebuffer(length);
            int read = 0;
            while (read < length)
            {
                read += await file.ReadAsync(messagebuffer.AsMemory(read, length - read));
            }
            return await Processing.Deserialize(new ReadOnlyMemory<byte>(messagebuffer, 0, length));
        }
        /// <summary>
        /// Append message to the end of file. Stream position remains unchanged.
        /// </summary>
        /// <param name="message">Message to be appended.</param>
        /// <returns>Async Task that completes with bool.</returns>
        public async Task<bool> AppendMessage(Message message)
        {
            long initialposition = file.Position;
            byte[]? data = await Processing.Serialize(message);
            if (data != null)
            {
                file.Position = file.Length;
                await WriteInt(data.Length);
                await file.WriteAsync(data);
                await file.FlushAsync();
                file.Position = sizeof(int);
                file.Position = initialposition;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Iterates over messages in file, but won't delete them.
        /// </summary>
        /// <returns>IAsyncEnumerable.</returns>
        public async IAsyncEnumerable<Message> GetMessages()
        {
            while (file.Position < file.Length)
            {
                yield return await ReadMessage();
            }
            file.Position = start;
            yield break;
        }
        /// <summary>
        /// Iterates over messages in file and deletes it.
        /// </summary>
        /// <returns>IAsyncEnumerable.</returns>
        public async IAsyncEnumerable<Message> GetMessagesAndDelete()
        {
            while (file.Position < file.Length)
            {
                yield return await ReadMessage();
            }
            await Delete();
            yield break;
        }
        /// <summary>
        /// Deletes all messages.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task Delete()
        {
            await Close();
            System.IO.File.Delete(path);
        }
        /// <summary>
        /// Closes DataHandler.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task Close()
        {
            await file.FlushAsync();
            file.Close();
            await file.DisposeAsync();
        }
    }
}
