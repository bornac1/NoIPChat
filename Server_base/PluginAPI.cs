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
        public Server Server { get; set; }
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
        public Task ClientAcceptedAsync(in Client client)
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
        public void GetServerInfo(in string localip, in string remoteip, in int remoteport, in int timeout)
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
    }
}
