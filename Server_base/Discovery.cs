using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
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
                        if (srv.LocalIP != localip || srv.RemoteIP != serverip || srv.RemotePort != serverport)
                        {
                            //Server data has changed

                            //Update values
                            srv.LocalIP = localip;
                            srv.RemoteIP = serverip;
                            srv.RemotePort = serverport;
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
        private void Close()
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
