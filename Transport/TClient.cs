using System.Net;

namespace Transport
{
    public class TClient : IDisposable
    {
        /// <summary>
        /// Saves protocol information.
        /// </summary>
        private readonly Protocol protocol;
        /// <summary>
        /// Saves TcpClient if protocol is TCP. It should be checked that != null before every usage.
        /// </summary>
        private readonly TcpClient? tcpClient;
        // Every new protocol field should be here

        /// <summary>
        /// Default constructor just for protocol. It will not initialise protocol-specific field.
        /// </summary>
        /// <param name="protocol">Protocol.</param>
        public TClient(Protocol protocol)
        {
            this.protocol = protocol;
        }
        /// <summary>
        /// Constructor used for TCP protocol.
        /// </summary>
        /// <param name="tcpClient"></param>
        public TClient(TcpClient tcpClient)
        {
            protocol = Protocol.TCP;
            this.tcpClient = tcpClient;
        }
        /// <summary>
        /// Synchronously connects using TCP.
        /// </summary>
        /// <remarks>Other protocols should implement their own Connect.</remarks>
        /// <param name="address">IP address.</param>
        /// <param name="port">Port.</param>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Other protocols can't use TCP connect.</exception>
        public void Connect(IPAddress address, int port)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    tcpClient.Connect(address, port);
                }
                else
                {
                    throw new NullReferenceException();
                }

            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Asynchronously connects using TCP.
        /// </summary>
        /// <remarks>Other protocols should implement their own ConnectAsync.</remarks>
        /// <param name="address">IP address.</param>
        /// <param name="port">Port.</param>
        /// <returns>Task.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Other protocols can't use TCP connect.</exception>
        public Task ConnectAsync(IPAddress address, int port)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    return tcpClient.ConnectAsync(address, port);
                }
                else
                {
                    throw new NullReferenceException();
                }

            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Closes TCP connection.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public void Close()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    tcpClient.Close();
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Disposes resources.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public void Dispose()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    tcpClient.Dispose();
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Receives data synchronously.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <param name="offset">The location in buffer to store data.</param>
        /// <param name="count">The number of bytes to be received.</param>
        /// <returns>Number of bytes received.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public int Receive(byte[] buffer, int offset, int count)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    return tcpClient.Receive(new Span<byte>(buffer,offset,count));
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Receives data synchronously.
        /// </summary>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <returns>Number of bytes received.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public int Receive(Span<byte> buffer)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    return tcpClient.Receive(buffer);
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Receives data asynchronously.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <param name="offset">The location in buffer to store data.</param>
        /// <param name="count">The number of bytes to be received.</param>
        /// <returns>Task that completes with number of bytes received.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    return await tcpClient.ReceiveAsync(new Memory<byte>(buffer, offset, count));
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Receives data asynchronously.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <param name="buffer">Used for stroring received data.</param>
        /// <returns>Task that completes with number of bytes received.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public async Task<int> ReceiveAsync(Memory<byte> buffer)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    return await tcpClient.ReceiveAsync(buffer);
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Sends data synchronously. Will send all data.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <param name="data">Contains data to be sent.</param>
        /// <returns>Number of bytes sent.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public int Send(ReadOnlySpan<byte> data)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    int tsent = 0;
                    while (data.Length > 0)
                    {
                        int sent = tcpClient.Send(data);
                        if (data.Length == 0)
                        {
                            break;
                        }
                        data = data[sent..];
                        tsent += sent;
                    }
                    return tsent;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        /// <summary>
        /// Sends data asynchronously. Will send all data.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <param name="data">Contains data to be sent.</param>
        /// <returns>Task that completes with number of bytes sent.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        public async Task<int> SendAsync(ReadOnlyMemory<byte> data)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    int tsent = 0;
                    while (data.Length > 0)
                    {
                        int sent = await tcpClient.SendAsync(data);
                        if (data.Length == 0)
                        {
                            break;
                        }
                        data = data[sent..];
                        tsent += sent;
                    }
                    return tsent;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        //Let's have finaliser just in case.
        ~TClient()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    tcpClient.Dispose();
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
