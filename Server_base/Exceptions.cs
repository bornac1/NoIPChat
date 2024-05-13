namespace Server_base
{
    /// <summary>
    /// File version exception.
    /// </summary>
    public class VersionException : Exception
    {
        /// <summary>
        /// File version Exception.
        /// </summary>
        public VersionException() { }
        /// <summary>
        /// File version Exception.
        /// </summary>
        public VersionException(string message) : base(message)
        {
        }
        /// <summary>
        /// File version Exception.
        /// </summary>
        public VersionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    /// <summary>
    /// File error.
    /// </summary>
    public class FileException : Exception
    {
        /// <summary>
        /// File error.
        /// </summary>
        public FileException() { }
        /// <summary>
        /// File error.
        /// </summary>
        public FileException(string message) : base(message)
        {
        }
        /// <summary>
        /// File error.
        /// </summary>
        public FileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
