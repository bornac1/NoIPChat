namespace Client
{
    /// <summary>
    /// Plugin interface
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Client field.
        /// </summary>
        public Client? Client { get; set; }
        /// <summary>
        /// True if plugin is patch, false if not.
        /// </summary>
        public bool IsPatch { get { return false; } }
        /// <summary>
        /// Run during plugin initialization.
        /// </summary>
        /// <returns>Async Task.</returns>
        public Task Initialize()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when the plugin throws Exception.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <returns>Async Task.</returns>
        public Task WriteLog(Exception ex)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when Client throws Exception.
        /// </summary>
        /// <param name="ex">Exception.</param>
        /// <returns>Async Task.</returns>
        public Task ClientLog(Exception ex)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Run when Client receives messsage.
        /// </summary>
        /// <param name="message">Message object.</param>
        /// <returns>Async Task.</returns>
        public Task MessageReceived(Messages.Message message)
        {
            throw new NotImplementedException();
        }
    }
}
