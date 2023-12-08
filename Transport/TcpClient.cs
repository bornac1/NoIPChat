using System;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class TcpClient :IDisposable
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
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            return await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count));
        }
        public async Task SendAsync(byte[] buffer, int offset, int count)
        {
            await socket.SendAsync(new ArraySegment<byte>(buffer, offset, count));
        }
    }
}
