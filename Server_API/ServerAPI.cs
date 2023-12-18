using Configuration;
using Messages;
using Sodium;

namespace Server
{
    public class ServerAPI
    {
        private Server? server;
        /// <summary>
        /// Initializes Server API.
        /// This API is used to create and interact with new Server instance.
        /// It is NOT used to interact with Server instance already running in separate process or on separate machine.
        /// </summary>
        public ServerAPI()
        {

        }
        ///<summary
        ///>Creates new Server.
        ///</summary>
        ///<param name="name">Name of the server.</param>
        ///<param name="interfaces">List of netwok interfaces used by server</param>
        public void CreateServer(string name, List<Interface> interfaces, KeyPair ecdh)
        {
            server = new Server(name, interfaces, ecdh);
        }
        ///<summary>
        ///Sends message from server to user.
        ///</summary>
        ///<param name="user">Username of the user.</param>
        ///<param name="message">Message to be sent.</param>
        public async Task SendMessageToUser(string user, Message message)
        {
            if (server != null)
            {
                if (StringProcessing.GetServer(user) == server.name)
                {
                    await server.SendMessageThisServer(user, message);
                }
                else
                {
                    await server.SendMessageOtherServer(user, message);
                }
            }
        }
        ///<summary>
        ///Sends message from server to known remote server.
        ///</summary>
        ///<param name="remote">Name of the remote serve.r</param>
        ///<param name="message">Message to be sent.</param>
        /*public async Task SendMessageToRemoteServer(string remote, Message message)
        {
            if (server != null)
            {
                await server.SendMessageRemote(remote, message);
            }
        }*/
        /// <summary>
        /// Checks if remote server is in known server list in memory.
        /// </summary>
        /// <param name="name">Name of the remote server.</param>
        /// <returns>True if server is known, false if not.</returns>
        public bool IsKnownServer(string name)
        {
            if (server != null)
            {
                return server.GetServer(name).Item1;
            }
            return false;
        }
        /// <summary>
        /// Closes current server and disconnects all.
        /// </summary>
        public async Task CloseServer()
        {
            if (server != null)
            {
                await server.Close();
            }
        }
        /// <summary>
        /// Loads know servers from file to memory. Doesn't have to be called, as they are loaded automatically when new Server is created.
        /// </summary>
        public async Task LoadKnownServers()
        {
            if (server != null)
            {
                await server.LoadServers();
            }
        }
        /// <summary>
        /// Saves know servers from memory to file. Doesn't have to be called, as they are saved automatically when server is closed.
        /// </summary>
        public async Task SaveServers()
        {
            if (server != null)
            {
                await server.SaveServers();
            }
        }
        /// <summary>
        /// Adds new server to know server list in memory.
        /// </summary>
        /// <param name="name">Name of the remote server.</param>
        /// <param name="localIP">IP address on this server that is used to connect to remote server.</param>
        /// <param name="remoteIP">IP address of remote server.</param>
        /// <param name="remoteport">Port on the remote server.</param>
        /// <param name="timeout">Timeout of connection in seconds. After timeout, connection is terminated automatically.</param>
        /// <returns>True if added, fales if not.</returns>
        public bool AddKnownServer(string name, string localIP, string remoteIP, int remoteport, int timeout)
        {
            if (server != null)
            {
                Servers srv = new()
                {
                    Name = name,
                    LocalIP = localIP,
                    RemoteIP = remoteIP,
                    RemotePort = remoteport,
                    TimeOut = timeout
                };
                return server.servers.TryAdd(name, srv);
            }
            return false;
        }
        /// <summary>
        /// Removes server from know server list in memory.
        /// </summary>
        /// <param name="name">Name of the remote server.</param>
        /// <returns>True if removed, false if not.</returns>
        public bool RemoveKnownServer(string name)
        {
            if (server != null)
            {
                return server.servers.TryRemove(name, out _);
            }
            return false;
        }
        /// <summary>
        /// Adds new message to the list. Message will be send as soon as client connects.
        /// </summary>
        /// <param name="name">Username of the user.</param>
        /// <param name="message">Message to be added.</param>
        /// <returns>True if added, false if not.</returns>
        public bool AddMessageClient(string name, Message message)
        {
            if (server != null)
            {
                return server.AddMessages(name, message);
            }
            return false;
        }
        /// <summary>
        /// Removes all messages for the user in list there were not sent.
        /// </summary>
        /// <param name="name">Username of the user.</param>
        /// <returns>true if removed, false if not.</returns>
        public bool RemoveAllMessagesClient(string name)
        {
            if (server != null)
            {
                return server.messages.TryRemove(name, out _);
            }
            return false;
        }
    }
}