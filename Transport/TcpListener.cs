using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class TcpListener : IListener
    {
        private readonly Socket socket;
        public TcpListener(IPAddress localaddr, int port)
        {
            try
            {
                socket = new Socket(localaddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(localaddr, port));
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public void Start()
        {
            try
            {
                socket.Listen();
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
    public TcpClient Accept()
        {
            try
            {
                return new(socket.Accept());
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public async Task<TcpClient> AcceptAsync()
        {
            try
            {
                return new(await socket.AcceptAsync());
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public void Stop()
        {
            socket.Close();
        }
        public void Dispose()
        {
            socket.Dispose();
            GC.SuppressFinalize(this);
        }
        ~TcpListener()
        {
            socket.Dispose();
        }
    }
}
