namespace Server_base
{
    public class VersionException : Exception
    {
        public VersionException() { }
        public VersionException(string message) : base(message)
        {
        }
        public VersionException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class FileException : Exception
    {
        public FileException() { }
        public FileException(string message) : base(message)
        {
        }
        public FileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
