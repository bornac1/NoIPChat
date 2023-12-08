namespace Transport
{
    public class TListener : IDisposable
    {
        /// <summary>
        /// Saves protocol information.
        /// </summary>
        private readonly Protocol protocol;
        /// <summary>
        /// Saves TcpClient if protocol is TCP. It should be checked that != null before every usage.
        /// </summary>
        private readonly TcpListener? tcpListener;
        // Every new protocol field should be here

        /// <summary>
        /// Default constructor just for protocol. It will not initialise protocol-specific field.
        /// </summary>
        /// <param name="protocol">Protocol.</param>
        public TListener(Protocol protocol)
        {
            this.protocol = protocol;
        }
        /// <summary>
        /// Constructor used for TCP protocol.
        /// </summary>
        /// <param name="tcpClient"></param>
        public TListener(TcpListener tcpListener)
        {
            this.tcpListener = tcpListener;
        }
        /// <summary>
        /// Starts accepting connections.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Other protocols can't use TCP constructor.</exception>
        public void Start()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpListener != null)
                {
                    tcpListener.Start();
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
        /// Accepts new connection synchronously.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <returns>TClient.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public TClient Accept()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpListener != null)
                {
                    return new(tcpListener.Accept());
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
        /// Accepts new connection asynchronously.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <returns>Task that completes with TClient.</returns>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public async Task<TClient> AcceptAsync()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpListener != null)
                {
                    return new(await tcpListener.AcceptAsync());
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
        /// Stops accepting connections.
        /// </summary>
        /// <remarks>Here should other protocols be implemented.</remarks>
        /// <exception cref="NullReferenceException">Protocol specific field is null.</exception>
        /// <exception cref="NotImplementedException">Protocol is not implemented.</exception>
        public void Stop()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpListener != null)
                {
                    tcpListener.Stop();
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
                if (tcpListener != null)
                {
                    tcpListener.Dispose();
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
        //Let's have a finaliser just in case.
        ~TListener()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpListener != null)
                {
                    tcpListener.Dispose();
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
