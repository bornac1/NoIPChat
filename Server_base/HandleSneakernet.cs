using Messages;

namespace Server_base
{
    public partial class Server
    {
        //Handles loaded Sneakernet file
        /// <summary>
        /// Processes messages.
        /// </summary>
        /// <param name="message">Message to be processed.</param>
        /// <returns>Async Task.</returns>
        private async Task ProcessSneakernetMessage(Message message)
        {
            if (message.Users != null)
            {
                //Users message
                await ProcessSneakernetUsers(message.Users);
            }
            else if (message.User != null && message.Pass != null)
            {
                //Login message
                await ProcessSneakernetLoginMessage(message);
            }
            else if (message.Receiver != null && message.Auth != null)
            {
                //Auth message
                await ProcessSneakernetAuthMessage(message);
            }
            else if (message.Sender != null && message.Receiver != null)
            {
                //Mesage to be relayed
                await ProcessSneakernetRelayMessage(message);
            }
            else if (message.Receiver != null && message.Receiver == name)
            {
                //Message for this server
                ProcessSneakernetLocalMessage(message);
            }
        }
        private async Task ProcessSneakernetUsers(string users)
        {
            await Task.Run(() =>
            {
                foreach (string user in StringProcessing.GetUsersServer(users))
                {
                    if (remoteusers.TryAdd(name, user))
                    {
                        //Already exists
                    }
                }
            });
        }
        private async Task ProcessSneakernetRelayMessage(Message message)
        {
            if (message.Receiver != null)
            {
                if (MemoryExtensions.Equals(StringProcessing.GetServer(message.Receiver), name, StringComparison.OrdinalIgnoreCase))
                {
                    //This is message for user who's home server is this one
                    await SendMessageThisServer(message.Receiver, message);
                }
                else
                {
                    //This is message for user who's home server is other one
                    await SendMessageOtherServer(message.Receiver, message);
                }
            }
        }
        private async Task ProcessSneakernetLoginMessage(Message message)
        {
            if (message.User != null && message.Pass != null)
            {
                string srv = StringProcessing.GetServer(message.User).ToString();
                if (MemoryExtensions.Equals(srv, name, StringComparison.OrdinalIgnoreCase))
                {
                    //Users home server is this one

                    //Authenticate
                    if (message.Pass != null)
                    {
                        /*await SendMessage(new Message()
                        {
                            Sender = server.name,
                            Receiver = message.User,
                            Auth = true
                        });*/
                        if (!remoteusers.TryAdd(message.User, name))
                        {
                            //Already exists
                        }
                    }
                    //await SendAllMessagesRemoteUser(message.User);
                }
                else
                {
                    //Users home server is other one
                    //Multi hop
                    message.Sender = name;
                    message.Receiver = srv;
                    await SendMessageServer(srv, message, name);
                }
            }
        }
        private async Task ProcessSneakernetAuthMessage(Message message)
        {
            if (message.Receiver != null)
            {
                string srv = StringProcessing.GetServer(message.Receiver).ToString();
                if (!MemoryExtensions.Equals(srv, name, StringComparison.OrdinalIgnoreCase))
                {
                    //Just to make sure
                    if (clients.TryGetValue(message.Receiver, out Client? cli))
                    {
                        //User is connected to this server
                        await cli.SendMessage(message);
                    }
                    else
                    {
                        //User is not connected to this server
                        //Multi hop
                        await SendMessageServer(srv, message, name);
                    }
                }
            }
        }
        private void ProcessSneakernetLocalMessage(Message message)
        {
            if (message.User != null && message.Disconnect == true)
            {
                //Disconnect message
                if (!remoteusers.TryRemove(message.User, out _))
                {
                    //Is already disconnected
                    //Or wasn't connected at all
                }
            }
        }
    }
}
