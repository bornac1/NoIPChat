using Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public partial class Client
    {
        //Handles connected servers
        /// <summary>
        /// Processes messages received from connected servers.
        /// </summary>
        /// <param name="message">Message to be processed.</param>
        /// <returns>Async task.</returns>
        private async Task ProcessServerMessage(Message message)
        {
            if (message.User != null && message.Pass != null)
            {
                await LoginClient(message.User, message.Pass);
            }
            else if (message.Disconnect == true)
            {
                await Disconnect();
            }
            else if (message.Msg != null || message.Data != null)
            {
                //Will send whole message to recivers (Msg+Data)
                message.Auth = null;
                message.User = null;
                message.Pass = null;
                message.Disconnect = null;
                if (message.Receiver != null)
                {
                    string[] recivers = message.Receiver.Split(';');
                    //Split messages for each receiver
                    foreach (string reciver in recivers)
                    {
                        message.Receiver = reciver;
                        string[] rcv = reciver.Split("@");
                        if (rcv[1].Equals(server.name, StringComparison.CurrentCultureIgnoreCase))
                        {
                            //This is receiver's home server
                            await server.SendMessage(reciver, message);
                        }
                        else
                        {
                            //Receiver's home server is other
                        }
                    }
                }
            }
        }
    }
}
