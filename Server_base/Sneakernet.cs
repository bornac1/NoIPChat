using Messages;

namespace Server_base
{
    public partial class Server
    {
        //Implements sneakernet by saving messages to file
        //File is saved to the given path
        /// <summary>
        /// Saves data into sneakernet file.
        /// </summary>
        /// <param name="path">Path to file where data should be saved.</param>
        /// <param name="name">Name of the server for which data are beeing saved.</param>
        /// <returns>Async Task.</returns>
        public async Task SaveSneakernet(string path, string name)
        {
            DataHandler? handler = null;
            try
            {
                handler = await DataHandler.CreateSneakernet(path, SV);
                if (messages_server.TryGetValue(name, out DataHandler? handler1) && handler1 != null)
                {
                    await foreach (Message message in handler1.GetMessages())
                    {
                        await handler.AppendMessage(message);
                    }
                    await handler1.Delete();
                    messages_server.TryRemove(name, out _);
                    await handler.Close();
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    //File problem
                    //No need for logging
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
                if (handler != null)
                {
                    await handler.Close();
                }
            }
        }
        /// <summary>
        /// Loads data from sneakernet file.
        /// </summary>
        /// <param name="path">Path to file where data is saved.</param>
        /// <returns>Async Task.</returns>
        public async Task LoadSneakernet(string path)
        {
            DataHandler? handler = null;
            try
            {
                handler = await DataHandler.CreateSneakernet(path, SV);
                await foreach (Message message in handler.GetMessages())
                {
                    //Load message
                    await ProcessSneakernetMessage(message);
                }
                await handler.Close();
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    //File problem
                    //No need for logging
                }
                else
                {
                    //Logging
                    await WriteLog(ex);
                }
                if (handler != null)
                {
                    await handler.Close();
                }
            }
        }
    }
}
