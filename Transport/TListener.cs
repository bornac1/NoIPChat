namespace Transport
{
    public class TListener : IDisposable
    {
        private readonly Protocol protocol;
        private readonly TcpListener? tcpListener;
        public TListener(Protocol protocol)
        {
            this.protocol = protocol;
        }
        public TListener(TcpListener tcpListener)
        {
            this.tcpListener = tcpListener;
        }
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
        public void Dispose()
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpListener != null)
                {
                    tcpListener.Dispose();
                    GC.SuppressFinalize(this);
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
