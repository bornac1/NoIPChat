namespace Transport
{
    /// <summary>
    /// Interface for listeners using different transport methods.
    /// </summary>
    /// <remarks>
    /// Every class should also implement specific Accept and AcceptAsync methods that return their Client.
    /// </remarks>
    public interface IListener : IDisposable
    {
        /// <summary>
        /// Starts accepting new connections.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops accepting connections.
        /// </summary>
        void Stop();
    }
}
