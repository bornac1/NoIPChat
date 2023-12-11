using Messages;

namespace Server
{
    public partial class Client
    {
        //Handles locally connected clients
        /// <summary>
        /// Processes messages received from local clients.
        /// </summary>
        /// <param name="message">Message to be processed.</param>
        /// <returns>Async Task.</returns>
        private async Task ProcessClientMessage(Message message)
        {
            try
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
                                await server.SendMessageThisServer(reciver, message);
                            }
                            else
                            {
                                //Receiver's home server is other
                                await server.SendMessageOtherServer(reciver, message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Logging
                await Server.WriteLog(ex);
            }
        }
        /// <summary>
        /// Handles login of local client.
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Async Task.</returns>
        private async Task LoginClient(string user, string pass)
        {
            //Save username
            this.user = user;
            //Authenticate
            if (pass != string.Empty)
            {
                auth = true;
            }
            //Send auth message
            await SendMessage(new Message()
            {
                Auth = auth
            });
            //Send all saved messages
            await SendAllMessages();
            //Add to clients
            if (!server.clients.TryAdd(user, this))
            {
                //Already exsists
                if (server.clients.TryGetValue(user, out Client? cli) && cli != null)
                {
                    await cli.Disconnect();
                    //Try once again
                    if (!server.clients.TryAdd(user, this))
                    {
                        //Don't know why it would fail
                    }
                }
            }
        }
        private async Task DisconnectClient(bool force)
        {
            if (force && connected)
            {
                Message message1 = new()
                {
                    Disconnect = true
                };
                await SendMessage(message1);
            }
            if (!server.clients.TryRemove(user.ToLower(), out _))
            {
                //Probably already removed or not added at all
            }
            var usr = user.Split("@");
            if (usr[1] != server.name)
            {
                //User home server is remote
                await server.SendMessageServer(usr[1], new Message()
                {
                    Sender = server.name,
                    Receiver = usr[1],
                    User = user,
                    Disconnect = true
                });
            }
        }
    }
}
