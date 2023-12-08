namespace Transport
{
    /// <summary>
    /// Interface for clients using different transport methods.
    /// </summary>
    /// <remarks>
    /// Every class should also implement specific Connect and ConnectAsync methods with their specific parameters.
    /// There should be a constructor which will be called from Listener when new connection is accepted.
    /// </remarks>
    public interface IClient : IDisposable
    {
        /// <summary>
        /// Closes connection.
        /// </summary>
        void Close();
        /// <summary>
        /// Receives data synchronously.
        /// </summary>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <param name="offset">The location in buffer to store data.</param>
        /// <param name="count">The number of bytes to be received.</param>
        /// <returns>Number of bytes received.</returns>
        int Receive(byte[] buffer, int offset, int count);
        /// <summary>
        /// Receives data asynchronously.
        /// </summary>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <param name="offset">The location in buffer to store data.</param>
        /// <param name="count">The number of bytes to be received.</param>
        /// <returns>Task that completes with number of bytes received.</returns>
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count);
        /// <summary>
        /// Sends data synchronously.
        /// </summary>
        /// <param name="buffer">Contains data to be sent.</param>
        /// <param name="offset">The position in buffer at which to begin sending.</param>
        /// <param name="count">The number of bytes to be send.</param>
        /// <returns>Number of bytes sent.</returns>
        int Send(byte[] buffer, int offset, int count);
        /// <summary>
        /// Sends data asynchronously.
        /// </summary>
        /// <param name="buffer">Contains data to be sent.</param>
        /// <param name="offset">The position in buffer at which to begin sending.</param>
        /// <param name="count">The number of bytes to be send.</param>
        /// <returns>Task that completes with number of bytes sent.</returns>
        Task<int> SendAsync(byte[] buffer, int offset, int count);
    }
}
