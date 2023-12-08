using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        public TClient (TcpClient tcpClient)
        {
            protocol = Protocol.TCP;
            this.tcpClient = tcpClient;
        }
        public Task ConnectAsync(IPAddress address, int port)
        {
            if (protocol == Protocol.TCP && tcpClient != null)
            {
                return tcpClient.ConnectAsync(address, port);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void Close()
        {
            if (protocol == Protocol.TCP && tcpClient != null)
            {
                tcpClient.Close();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            if (protocol == Protocol.TCP && tcpClient != null)
            {
                tcpClient.Dispose();
            }
            else
            {
                throw new NotImplementedException();
            }
            GC.SuppressFinalize(this);
        }
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            if (protocol == Protocol.TCP && tcpClient != null)
            {
                return await tcpClient.ReceiveAsync(buffer, offset, count);
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
            if (protocol == Protocol.TCP && tcpClient != null)
            {
                await tcpClient.SendAsync(buffer, offset, count);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
