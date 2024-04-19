using ConfigurationData;
using Sodium;
namespace Server_interface
{
    public delegate Task WriteLogAsync(string message);
    public interface IServer
    {
        TaskCompletionSource<bool> Closed { get; set; }
        IServer CreateServer(string name, List<Interface> interfaces, KeyPair ecdh, WriteLogAsync? writelogasync);
        Task Close();
    }
}
