using System.Runtime.CompilerServices;
using Dynamic_software_update_test_interface;
namespace Dynamic_software_test_library
{
    public class Class1: IClass1
    {
        public void Print(string message)
        {
            Console.WriteLine("Loaded print " + message);
        }

    }
}
