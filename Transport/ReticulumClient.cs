using Python.Runtime;

namespace Transport
{
    public class ReticulumClient : IClient
    {
        ReticulumClient() {
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

        public int Receive(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public int Send(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public Task<int> SendAsync(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
