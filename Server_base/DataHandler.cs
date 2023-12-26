using Messages;

namespace Server
{
    public class DataHandler
    {
        private readonly FileStream file;
        private readonly byte[] intbuffer = new byte[sizeof(int)];
        private byte[] messagebuffer = new byte[1024];
        private int version;
        private readonly string folder;
        private readonly string name;
        private DataHandler(string folder, string name) {
            this.folder = folder;
            this.name = string.Join('.',name,"bin");
            string path = Path.Combine(this.folder, this.name);
            Directory.CreateDirectory(this.folder);
            file = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }
        /// <summary>
        /// Creates DataHandler for Data file.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="version">Server version.</param>
        /// <returns>Task that completes with DataHandler.</returns>
        /// <exception cref="VersionException">File version is newer than server.</exception>
        public static async Task<DataHandler> CreateData(string name, int version)
        {
            DataHandler handler = new("Data", name);
            if (handler.file.Length >= sizeof(int))
            {
                handler.version = await handler.ReadInt();
                if (handler.version > version)
                {
                    throw new VersionException("File version is newer than server.");
                }
            }
            else
            {
                handler.version = version;
                await handler.WriteInt(version);
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
        public static async Task<DataHandler> CreateTemp(string name, int version)
        {
            DataHandler handler = new("Temp", name);
            if (handler.file.Length >= sizeof(int))
            {
                handler.version = await handler.ReadInt();
                if (handler.version > version)
                {
                    throw new VersionException("File version is newer than server.");
                }
            }
            else
            {
                handler.version = version;
                await handler.WriteInt(version);
            }
            return handler;
        }
        private async Task<int> ReadInt()
        {
            int read = await file.ReadAsync(intbuffer, 0, intbuffer.Length);
            while(read < intbuffer.Length)
            {
                read += await file.ReadAsync(intbuffer, read, intbuffer.Length);
            }
            return BitConverter.ToInt32(intbuffer, 0);
        }
        private async Task WriteInt(int data)
        {
            byte[] bdata = BitConverter.GetBytes(data);
            await file.WriteAsync(bdata, 0, bdata.Length);
        }
        private void Handlemessagebuffer(int size)
        {
            if(messagebuffer.Length >= size)
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
                if(size % messagebuffer.Length != 0)
                {
                    ns += 1;
                }
                ns*=messagebuffer.Length;
                messagebuffer = new byte[ns];
            }
        }
        private async Task<Message> ReadMessage()
        {
            int length = await ReadInt();
            Handlemessagebuffer(length);
            int read = await file.ReadAsync(messagebuffer, 0, length);
            while(read < length)
            {
                read += await file.ReadAsync(messagebuffer, read, length);
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
            if(data != null)
            {
                byte[] length = BitConverter.GetBytes(data.Length);
                file.Position = file.Length;
                await file.WriteAsync(length, 0, length.Length);
                await file.WriteAsync(data, 0, data.Length);
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
            file.Position = sizeof(int);//go to first messsage
            while (file.Position < file.Length)
            {
                yield return await ReadMessage();
            }
            yield break;
        }
        /// <summary>
        /// Iterates over messages in file and deletes it.
        /// </summary>
        /// <returns>IAsyncEnumerable.</returns>
        public async IAsyncEnumerable<Message> GetMessagesAndDelete()
        {
            file.Position = sizeof(int);//go to first messsage
            while (file.Position < file.Length)
            {
                yield return await ReadMessage();
            }
            await Close();
            System.IO.File.Delete(Path.Combine(folder, name));
            yield break;
        }
        /// <summary>
        /// Deletes all messages.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task Delete()
        {
            await Close();
            System.IO.File.Delete(Path.Combine(folder, name));
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
