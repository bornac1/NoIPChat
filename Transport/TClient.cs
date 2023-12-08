using System.Net;

namespace Transport
{
    public class TClient : IDisposable
    {
        private readonly Protocol protocol;
        private readonly TcpClient? tcpClient;
        public TClient(Protocol protocol)
        {
            this.protocol = protocol;
        }
        public TClient(TcpClient tcpClient)
        {
            protocol = Protocol.TCP;
            this.tcpClient = tcpClient;
        }
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
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    return await tcpClient.ReceiveAsync(buffer, offset, count);
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
        public async Task SendAsync(byte[] buffer)
        {
            await SendAsync(buffer, 0, buffer.Length);
        }
        public async Task SendAsync(byte[] buffer, int offset, int count)
        {
            if (protocol == Protocol.TCP)
            {
                if (tcpClient != null)
                {
                    await tcpClient.SendAsync(buffer, offset, count);
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
