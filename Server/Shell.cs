using System;

namespace Server
{
    internal class Shell
    {
        //Server command line interface
        private Program program;
        //Commands
        private readonly Dictionary<string, Action<string[]?>> commandActions = new();
        public Shell(Program program) {
            this.program = program;
            commandActions.Add("exit", Exit);
            commandActions.Add("stop server", StopServer);
            Loop();
        }
        private void Loop () {
            while (true)
            {
                ProcessCommand(Console.ReadLine());
            }
        }
        public void ProcessCommand(string userInput)
        {
            string command = string.Empty;
            if (userInput != null)
            {
                command  = userInput;
            }
            string[]? args = null;
            if (commandActions.TryGetValue(command, out Action<string[]?>? action))
            {
                action?.Invoke(args);
            }
            else
            {
                Console.WriteLine("Unknown command.");
            }
        }
        private void Exit(string[]? args)
        {
            Environment.Exit(0);
        }
        private async void StopServer(string[]? args)
        {
            if (program.server != null)
            {
                await program.server.Close();
                program.server = null;
                Console.WriteLine("Server stoped.");
            }
        }
    }
}
