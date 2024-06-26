﻿using System.Net;
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
            catch (Exception ex)
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
            }
            catch (Exception ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public async Task ConnectAsync(IPAddress address, int port)
        {
            try
            {
                await socket.ConnectAsync(address, port);
            }
            catch (Exception ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public void Close(bool force = false)
        {
            if (force)
            {
                socket.Close();
            }
            else
            {
                socket.LingerState = new LingerOption(true, 0);
                socket.Close();
            }
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
        public int Receive(Span<byte> buffer)
        {
            try
            {
                return socket.Receive(buffer, SocketFlags.None);
            }
            catch (Exception ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public async Task<int> ReceiveAsync(Memory<byte> buffer)
        {
            try
            {
                return await socket.ReceiveAsync(buffer, SocketFlags.None);
            }
            catch (Exception ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public int Send(ReadOnlySpan<byte> data)
        {
            try
            {
                return socket.Send(data, SocketFlags.None);
            }
            catch (Exception ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
        public async Task<int> SendAsync(ReadOnlyMemory<byte> data)
        {
            try
            {
                return await socket.SendAsync(data, SocketFlags.None);
            }
            catch (Exception ex)
            {
                throw new TransportException("Socket exception", ex);
            }
        }
    }
}
