using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class TcpClient : IClient
    {
        private readonly Socket socket;
        public TcpClient(IPEndPoint localEP)
        {
            socket = new Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
        public TcpClient(Socket socket)
        {
            this.socket = socket;
        }
        public void Connect(IPAddress address, int port)
        {
            socket.Connect(address, port);
        }
        public Task ConnectAsync(IPAddress address, int port)
        {
            return socket.ConnectAsync(address, port);
        }
        public void Close()
        {
            socket.Close();
        }
        public void Dispose()
        {
            socket.Dispose();
            GC.SuppressFinalize(this);
        }
        ~TcpClient()
        {
            socket.Dispose();
        }
        public int Receive(byte[] buffer, int offset, int count)
        {
            return socket.Receive(buffer, offset, count, SocketFlags.None);
        }
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            return await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
        }
        public int Send(byte[] buffer, int offset, int count)
        {
            return socket.Send(buffer, offset, count, SocketFlags.None);
        }
        public async Task<int> SendAsync(byte[] buffer, int offset, int count)
        {
            return await socket.SendAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
        }
    }
}
