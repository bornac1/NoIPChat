using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using ConfigurationData;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Server_base
{
    /// <summary>
    /// Handles UDP discovery packets.
    /// </summary>
    //Discovery packet is UDP
    //Is ipv6 as bool
    //Server public IP as 4 (ipv4) or 16 (ipv6) byte array
    //Server port as int32
    //Is server's name complete as bool
    //Server name as utf8 string (min 0, max 490 bytes)
    //Total size of packet: 10(min) to 512(max)
    public class Discovery
    {
        //Standard port
        private readonly int port = 3092;
        private readonly Server server;
        private readonly UdpClient[] listeners;
        private bool active = true;
        /// <summary>
        /// Discovery constructor.
        /// </summary>
        /// <param name="server">Server object.</param>
        public Discovery(Server server)
        {
            this.server = server;
            listeners = new UdpClient[server.interfaces.Count];
            for (int i = 0;i<server.interfaces.Count;i++)
            {
                if (IPAddress.TryParse(server.interfaces[i].InterfaceIP, out IPAddress? ip)){
                    UdpClient c = new(new IPEndPoint(ip, port));
                    listeners[i] = c;
                    _= Receive(c);
                }
            }
        }
        private async Task Receive(UdpClient client)
        {
            while (active)
            {
                var data = await client.ReceiveAsync();
                ReadOnlyMemory<byte> message = data.Buffer;
                IPAddress? local = ((IPEndPoint?)client.Client.LocalEndPoint)?.Address;
                if (local != null)
                {
                    await ParseMessage(message, local.ToString());
                } else
                {
                    //Shouldn't happen
                    Console.WriteLine("Error null");
                }
            }
        }
        private async Task ParseMessage(ReadOnlyMemory<byte> message, string localip) {
            if (message.Length > 0)
            {
                bool ipv6 = BitConverter.ToBoolean(message[..1].Span);
                string? serverip = null;
                int serverport = 0;
                bool servernamecomplete = false;
                string? servername = null;
                if (ipv6 && message.Length > 21)
                {
                    serverip = new IPAddress(message.Slice(1, 16).Span).ToString();
                    serverport = BitConverter.ToInt32(message.Slice(17, 4).Span);
                    servernamecomplete = BitConverter.ToBoolean(message.Slice(21, 1).Span);
                    if (message.Length > 22)
                    {
                        servername = Encoding.UTF8.GetString(message[22..].Span);
                    }
                } else if(!ipv6 && message.Length > 9)
                {
                    serverip = new IPAddress(message.Slice(1, 4).Span).ToString();
                    serverport = BitConverter.ToInt32(message.Slice(5, 4).Span);
                    servernamecomplete = BitConverter.ToBoolean(message.Slice(9, 1).Span);
                    if (message.Length > 10)
                    {
                        servername = Encoding.UTF8.GetString(message[10..].Span);
                    }
                }
                if (servername != null && serverip != null && serverport > 0)
                {
                    if (server.servers.TryGetValue(servername, out Servers? srv) && srv != null)
                    {
                        Servers current = srv;
                        if (srv.LocalIP != localip || srv.RemoteIP != serverip || srv.RemotePort != serverport)
                        {
                            //Server data has changed

                            //Update values
                            srv.LocalIP = localip;
                            srv.RemoteIP = serverip;
                            srv.RemotePort = serverport;
                            if(!server.servers.TryUpdate(servername, srv, current))
                            {
                                //Don't know why it would fail
                            }
                        }
                    }
                    else
                    {
                        //Server is unknow
                        //Try to add it
                        if (!server.servers.TryAdd(servername, new Servers() { Name = servername, LocalIP = localip, RemoteIP = serverip, RemotePort = serverport, TimeOut = 0 }))
                        {
                            //Don't know why it would fail
                        }
                    }
                    if (!servernamecomplete)
                    {
                        //Connect to server to get full name
                        Client cli = await Client.CreateAsync(server,servername,localip,serverip,serverport,0);
                        if (!server.remoteservers.TryAdd(servername, cli))
                        {
                            //Don't know why
                        }
                    }
                }
            }
        }
        private static IPAddress GetBroadcastAddress(IPAddress ip, IPAddress? subnetMask)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && subnetMask != null) // IPv4
            {
                byte[] ipBytes = ip.GetAddressBytes();
                byte[] maskBytes = subnetMask.GetAddressBytes();

                if (ipBytes.Length != maskBytes.Length)
                {
                    throw new ArgumentException("IP address and subnet mask lengths do not match.");
                }

                byte[] broadcastBytes = new byte[ipBytes.Length];
                for (int i = 0; i < ipBytes.Length; i++)
                {
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                }

                return new IPAddress(broadcastBytes);
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
            {
                // No broadcast address for IPv6. Use the all-nodes multicast address.
                return IPAddress.Parse("ff02::1");
            }
            else
            {
                throw new NotSupportedException("Unsupported address family.");
            }
        }
        private async Task<byte[]?> CreateDiscoveryPacket(IPAddress serverIP, int port, string name)
        {
            try
            {
                // Packet details
                bool isIPv6 = serverIP.AddressFamily == AddressFamily.InterNetworkV6;
                bool isServerNameComplete = true; // Assume the server's name is complete.
                byte[] nameBytes = Encoding.UTF8.GetBytes(name);

                // Encode the packet
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                // 1. Is IPv6 (1 byte)
                writer.Write(isIPv6);

                // 2. Server public IP (4 bytes for IPv4, 16 bytes for IPv6)
                byte[] ipBytes = serverIP.GetAddressBytes();
                writer.Write(ipBytes);

                // 3. Server port (4 bytes)
                writer.Write(port);

                if (nameBytes.Length > 490)
                {
                    isServerNameComplete = false;
                }
                // 4. Is server name complete (1 byte)
                writer.Write(isServerNameComplete);
                // 5. Server name (UTF-8 encoded, up to 490 bytes)
                writer.Write(nameBytes);

                return stream.ToArray();
            } catch (Exception ex)
            {
                await server.WriteLog(ex);
            }
            return null;
        }
        private static IPAddress? GetSubnetMask(Interface inf)
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastInfo in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastInfo.Address.Equals(inf.InterfaceIP))
                    {
                        return unicastInfo.IPv4Mask;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Discovers new servers from current interfaces.
        /// </summary>
        /// <returns>Async Task.</returns>
        public async Task DiscoverNew()
        {

            foreach (var listener in listeners)
            {
                try
                {
                    var inf = server.interfaces.Where(i => i.InterfaceIP == listener.Client?.LocalEndPoint?.ToString()).FirstOrDefault();
                    if (inf != null && IPAddress.TryParse(inf.IP, out IPAddress? ip) && ip != null)
                    {
                        byte[]? message = await CreateDiscoveryPacket(ip, inf.Port, server.name);
                        IPAddress? subnetMask = GetSubnetMask(inf);
                        if (listener.Client.LocalEndPoint is IPEndPoint endpoint && (subnetMask != null || endpoint.AddressFamily == AddressFamily.InterNetworkV6))
                        {
                            // Determine the broadcast address for the interface
                            var broadcastAddress = GetBroadcastAddress(endpoint.Address, subnetMask);
                            if (message != null)
                            {
                                await listener.SendAsync(message, message.Length, new IPEndPoint(broadcastAddress, port));
                            }
                        }
                    }
                } catch (Exception ex)
                {
                    await server.WriteLog(ex);
                }
            }
        }
        /// <summary>
        /// Close and dispose Discovery.
        /// </summary>
        public void Close()
        {
            active = false;
            foreach(UdpClient c in listeners)
            {
                c.Close();
                c.Dispose();
            }
        }
    }
}
