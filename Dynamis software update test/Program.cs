using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Dynamic_software_update_test_interface;

namespace Dynamis_software_update_test
{
    internal class Program
    {
        delegate void PrintDelegate(string message);
        WeakReference contextref;

        AssemblyLoadContext context;
        Type type;
        IClass1 class1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Load()
        {
            context = new(null, true);
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assembly loaded = context.LoadFromAssemblyPath(Path.Combine(path,"Dynamic software test library.dll"));
            type = loaded.GetType("Dynamic_software_test_library.Class1");
            contextref = new(context);
            class1 = CreateClass1();
            class1.Print("Probna poruka");
        }
        private IClass1 CreateClass1()
        {
            return (IClass1)Activator.CreateInstance(type);
        }
        private void Unload()
        {
            class1 = null;
            type = null;
            context.Unload();
            context = null;
            for (int i = 0; contextref.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine($"Unload success: {!contextref.IsAlive}");
        }
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Load();
            program.Unload();
            Console.ReadLine();
        }
    }
}
