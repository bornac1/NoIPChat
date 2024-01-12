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
        void Close(bool force);
        /// <summary>
        /// Receives data synchronously.
        /// </summary>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <returns>Number of bytes received.</returns>
        int Receive(Span<byte> buffer);
        /// <summary>
        /// Receives data asynchronously.
        /// </summary>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <returns>Task that completes with number of bytes received.</returns>
        Task<int> ReceiveAsync(Memory<byte> buffer);
        /// <summary>
        /// Sends data synchronously.
        /// </summary>
        /// <param name="data">Contains data to be sent.</param>
        /// <returns>Number of bytes sent.</returns>
        int Send(ReadOnlySpan<byte> data);
        /// <summary>
        /// Sends data asynchronously.
        /// </summary>
        /// <param name="data">Contains data to be sent.</param>
        /// <returns>Task that completes with number of bytes sent.</returns>
        Task<int> SendAsync(ReadOnlyMemory<byte> data);
    }
}
