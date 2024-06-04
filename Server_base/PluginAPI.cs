using Messages;

namespace Server_base
{
    /// <summary>
    /// Plugin interface
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Server field.
        /// </summary>
        public Server? Server { get; set; }
        /// <summary>
        /// True if plugin is patch, false if not.
        /// </summary>
        public bool IsPatch { get { return false; } }
        /// <summary>
        /// Run during plugin initialization.
        /// </summary>
        public void Initialize()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run after server is initialised.
        /// </summary>
        public void ServerStart()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run after Servers are loaded.
        /// </summary>
        /// <returns>Async Task.</returns>
        public Task ServersLoadAsync()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run after Servers are saved,
        /// </summary>
        /// <returns>Async Task.</returns>
        public Task ServersSaveAsync()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when Client is accepted.
        /// </summary>
        /// <param name="client">Client</param>
        /// <returns>Async Task.</returns>
        public Task ClientAcceptedAsync(Client client)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Provides informations about known server.
        /// </summary>
        /// <param name="localip">Local interface IP used to contact to the server.</param>
        /// <param name="remoteip">IP of remote server.</param>
        /// <param name="remoteport">Port on remote server.</param>
        /// <param name="timeout">Timeout.</param>
        public void GetServerInfo(string localip, string remoteip, int remoteport, int timeout)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Overrides informations about known server.
        /// </summary>
        /// <param name="name">Name of the known server.</param>
        /// <returns>(bool, localip, remoteip, remoteport, timeout)</returns>
        public (bool, string, string, int, int) ReturnServerInfo(string name)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when the plugin throws Exception.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public void WriteLog(Exception ex)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when Server throws Exception.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public void ServerLog(Exception ex)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when Server is closed.
        /// </summary>
        public void Close()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when sending message to users for whom this is home server.
        /// </summary>
        /// <param name="user">Username.</param>
        /// <param name="message">Message.</param>
        /// <returns>Async Task that complets with bool. True if message should be sent by server, false if it's sent by plugin.</returns>
        public Task<bool> SendMessageThisServer(string user, Message message)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when sending message to users who's home server is other one
        /// </summary>
        /// <param name="user">Username.</param>
        /// <param name="message">Message.</param>
        /// <returns>Async Task that complets with bool. True if message should be sent by server, false if it's sent by plugin</returns>
        public Task<bool> SendMessageOtherServer(string user, Message message)
        {
            throw new NotImplementedException();
        }
    }
}
