using Python.Runtime;

namespace Transport
{
    public class ReticulumClient : IClient
    {
        PyModule scope;
        private bool closestarted = false;
        ReticulumClient()
        {
            PythonEngine.Initialize();
            Py.GIL();
            scope = Py.CreateScope().Exec("ReticulumClient.py");
        }
        public void Close()
        {
            PythonEngine.Shutdown();
        }

        public void Dispose()
        {
            PythonEngine.Shutdown();
            GC.SuppressFinalize(this);
        }
        ~ReticulumClient()
        {
            PythonEngine.Shutdown();
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
