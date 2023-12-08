using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
            if(protocol == Protocol.TCP && tcpListener != null)
            {
                tcpListener.Start();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public async Task<TClient> AcceptAsync()
        {
            if(protocol == Protocol.TCP && tcpListener != null) {
                return new (await tcpListener.AcceptAsync());            
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void Stop()
        {
            if (protocol == Protocol.TCP && tcpListener != null)
            {
                tcpListener.Stop();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void Dispose()
        {
            if (protocol == Protocol.TCP && tcpListener != null)
            {
                tcpListener.Dispose();
            }
            else
            {
                throw new NotImplementedException();
            }
            GC.SuppressFinalize(this);
        }
    }
}
