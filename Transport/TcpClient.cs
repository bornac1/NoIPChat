using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class TcpClient : IClient
    {
        private readonly Socket socket;
        public TcpClient(IPEndPoint localEP)
        {
            try
            {
                socket = new Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public TcpClient(Socket socket)
        {
            this.socket = socket;
        }
        public void Connect(IPAddress address, int port)
        {
            try
            {
                socket.Connect(address, port);
            } catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public Task ConnectAsync(IPAddress address, int port)
        {
            try
            {
                return socket.ConnectAsync(address, port);
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
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
            try
            {
                return socket.Receive(buffer, offset, count, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                return await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public int Send(byte[] buffer, int offset, int count)
        {
            try
            {
                return socket.Send(buffer, offset, count, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public async Task<int> SendAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                return await socket.SendAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
            }
            catch (SocketException ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
    }
}
