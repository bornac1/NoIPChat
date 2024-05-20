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
        public bool IsPatch { get; }
        /// <summary>
        /// Run during plugin initialization.
        /// </summary>
        public void Initialize()
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
        /// Run when Client throws Exception.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public void ClientLog(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
