namespace Server
{
    internal class Shell
    {
        //Server command line interface
        private Server server;
        //Commands strings
        private const string exit = "exit";
        public Shell(Server server) {
            this.server = server;
            Loop();
        }
        private void Loop () {
            while (true)
            {
                ProcessCommand(Console.ReadLine());
            }
        }
        private void ProcessCommand(string? command)
        {
            if (command != null)
            {
                ReadOnlySpan<char> comm = command.AsSpan();
                int i = 0;
                while (i < comm.Length)
                {
                    //skip whitespaces
                    while (comm[i] == ' ' && i < comm.Length)
                    {
                        i += 1;
                    }
                }
                ExecuteCommand(comm);
            }
        }
        private void ExecuteCommand (ReadOnlySpan<char> command)
        {
            if(MemoryExtensions.Equals(command, exit, StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }
        }
    }
}
