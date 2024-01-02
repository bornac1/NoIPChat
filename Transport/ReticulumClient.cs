using Python.Runtime;

namespace Transport
{
    public class ReticulumClient : IClient
    {
        ReticulumClient()
        {
            PythonEngine.Initialize();
        }
        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int Receive(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public Task<int> ReceiveAsync(Memory<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public int Send(ReadOnlySpan<byte> data)
        {
            throw new NotImplementedException();
        }

        public Task<int> SendAsync(ReadOnlyMemory<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
