using Server_base;
namespace Test_server_plugin
{
    public class Class1 : IPlugin
    {
        public Server Server { get; set; }
        public void Initialize()
        {
            Console.WriteLine("Test Server Plugin executed!");
        }
    }
}
