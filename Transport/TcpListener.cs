using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class TcpListener : IDisposable
    {
        private readonly Socket socket;
        public TcpListener(IPAddress localaddr, int port)
        {
            socket = new Socket(localaddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(localaddr, port));
        }
        public void Start()
        {
            socket.Listen();
        }
        public async Task<TcpClient> AcceptAsync()
        {
            return new(await socket.AcceptAsync());
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
    }
}
