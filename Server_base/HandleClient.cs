using Messages;

namespace Server_base
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
                if (aeskey != null)
                {
                    message = Encryption.DecryptMessage(message, aeskey);
                }
                if (message.User != null && message.Pass != null)
                {
                    await LoginClient(message);
                }
                else if (message.Disconnect == true)
                {
                    await Disconnect();
                }
                else if (message.Update == true)
                {
                    await ClientUpdate(message);
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
                        //Split messages for each receiver
                        foreach (string receiver in StringProcessing.GetReceivers(message.Receiver))
                        {
                            message.Receiver = receiver;
                            if (MemoryExtensions.Equals(StringProcessing.GetServer(receiver), server.name, StringComparison.OrdinalIgnoreCase))
                            {
                                //This is receiver's home server
                                await server.SendMessageThisServer(receiver, message);
                                ;
                            }
                            else
                            {
                                //Receiver's home server is other
                                await server.SendMessageOtherServer(receiver, message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Logging
                await server.WriteLog(ex);
            }
        }
        private static string? GetPatchName(string name)
        {
            //TODO: optimization
            //Format: 0.0.0 patch win-x64.nip
            string[] strings = name.Split(' ');
            if (strings.Length > 1)
            {
                return string.Join(' ', strings[0..1]) + ".nip";
            }
            return null;
        }
        private async Task ClientUpdate(Message message)
        {
            if (message.CV != null && message.Runtime != null)
            {
                if (message.CV < server.CVU)
                {
                    //Newer version is available
                    string? path = server.GetClientPatch(message.Runtime, message.CV);
                    if (path != null)
                    {
                        string? name = GetPatchName(Path.GetFileName(path));
                        if (name != null)
                        {
                            Messages.File file = new() { Name = name, Content = await System.IO.File.ReadAllBytesAsync(path) };
                            await SendMessage(new() { CVU = server.CVU, Update = true, Data = await Processing.SerializeFile(file) });

                        }
                    }
                    else
                    {
                        //No patch available
                        //Send update
                        if (server.clientupdates.TryGetValue(message.Runtime, out string? updatepath) && updatepath != null)
                        {
                            Messages.File file = new() { Name = Path.GetFileName(updatepath), Content = await System.IO.File.ReadAllBytesAsync(updatepath) };
                            await SendMessage(new() { CVU = server.CVU, Update = true, Data = await Processing.SerializeFile(file) });
                        }
                        else
                        {
                            //No update is available
                        }
                    }
                }
                else
                {
                    //Client has latest version
                    await SendMessage(new() { Update = true, CVU = message.CV });
                }
            }
        }
        /// <summary>
        /// Handles login of local client.
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Async Task.</returns>
        private async Task LoginClient(Message message)
        {
            if (message.User != null && message.Pass != null)
            {
                //Save username
                user = message.User;
                string usrserver = StringProcessing.GetServer(user).ToString();
                if (MemoryExtensions.Equals(usrserver, server.name, StringComparison.OrdinalIgnoreCase))
                {
                    //This is user home server
                    //Authenticate
                    if (message.Pass != null)
                    {
                        auth = true;
                    }
                    //Send auth message
                    await SendMessage(new Message()
                    {
                        Auth = auth,
                        CVU = server.CVU
                    });
                }
                else
                {
                    //User home server is remote
                    message.Sender = server.name;
                    message.Receiver = usrserver;
                    await server.SendMessageServer(usrserver, message);
                }
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
                /*else
                {
                    Console.WriteLine("client added " + server.clients.Count);
                }*/
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
            if (user != null)
            {
                if (!server.clients.TryRemove(user.ToLower(), out _))
                {
                    //Probably already removed or not added at all
                }
                string srv = StringProcessing.GetServer(user).ToString();
                if (!MemoryExtensions.Equals(srv, server.name, StringComparison.OrdinalIgnoreCase))
                {
                    //User home server is remote
                    await server.SendMessageServer(srv, new Message()
                    {
                        Sender = server.name,
                        Receiver = srv,
                        User = user,
                        Disconnect = true
                    });
                }
            }
        }
    }
}
