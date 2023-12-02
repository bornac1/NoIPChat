using Messages;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Test_client
{
    internal class Program
    {
        public bool auth = false;
        private static void Print(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                string byteString = b.ToString("X2"); // Convert to hexadecimal string
                Console.Write(byteString + " ");
            }
        }
        private async Task Receive(NetworkStream stream)
        {
            int bytesRead = 0;
            int bufferOffset = 0;
            byte[] buffer= new byte[1024];
            Processing processing = new Processing();
            while (true)
            {
                try
                {
                    int availableBytes = bytesRead - bufferOffset;

                    // Check if we have enough bytes in the buffer to read the size
                    if (availableBytes >= sizeof(int))
                    {
                        int messageSize = BitConverter.ToInt32(buffer, bufferOffset);
                        int totalMessageSize = sizeof(int) + messageSize;

                        // Check if the entire message fits in the buffer
                        if (totalMessageSize <= availableBytes)
                        {
                            byte[] messageBytes = new byte[messageSize];
                            Array.Copy(buffer, bufferOffset + sizeof(int), messageBytes, 0, messageSize);

                            // Move the remaining bytes in the buffer to the beginning
                            Array.Copy(buffer, bufferOffset + totalMessageSize, buffer, 0, availableBytes - totalMessageSize);

                            // Update the bytesRead and bufferOffset variables
                            bytesRead = availableBytes - totalMessageSize;
                            bufferOffset = 0;

                            //Message processing starts
                            try
                            {
                                Message message = await processing.Deserialize(messageBytes);
                                if (message.Auth == true)
                                {
                                    auth = true;
                                }
                                Console.WriteLine(message.Sender + ":" + message.Msg);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Message deseialization error");
                                Console.WriteLine(ex.ToString());
                            }
                        }
                    }

                    // Read more bytes from the stream
                    int bytesReadNow = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);

                    // Check if the stream has reached its end
                    if (bytesReadNow == 0)
                    {
                        break;
                    }

                    bytesRead += bytesReadNow;
                }
                catch (Exception ex)
                { //assume disconnection
                }
            }
        }
        static void Main(string[] args)
        {
            string username;
            TcpClient client= new TcpClient();
            Console.Write("Ip:");
            string ip = Console.ReadLine();
            Console.Write("Port:");
            string port = Console.ReadLine();
            client.Connect(IPAddress.Parse(ip), int.Parse(port));
            NetworkStream stream = client.GetStream();
            Processing processing = new Processing();
            Message message = new Message();
            message.CV = 1;
            Console.Write("Username:");
            username =  Console.ReadLine();
            message.User = username;
            Console.Write("Password:");
            message.Pass = Console.ReadLine();
            byte[] data = processing.Serialize(message).Result;
            stream.Write(BitConverter.GetBytes(data.Length));
            stream.Write(data);
            Program program = new Program();
            _ = program.Receive(stream);
            int c = 0;
            while (!program.auth)
            {
                Task.Delay(10);
                Console.WriteLine("Auth:"+c + program.auth);
                c++;
            }
            Console.WriteLine("Authenticated");
            string Msg = "";
            while (Msg != "kraj")
            {
                message = new Message();
                message.CV = 1;
                Console.Write("Reciver:");
                message.Receiver = Console.ReadLine();
                message.Sender = username;
                Msg = Console.ReadLine();
                message.Msg = Msg;
                if (Msg != "kraj") {
                    data = processing.Serialize(message).Result;
                    stream.Write(BitConverter.GetBytes(data.Length));
                    stream.Write(data);
                }
                else{
                    break;
                }
            }
            message = new Message();
            message.CV = 1;
            message.Disconnect = true;
            data = processing.Serialize(message).Result;
            stream.Write(BitConverter.GetBytes(data.Length));
            stream.Write(data);
            stream.Flush();
            stream.Close();
            stream.Dispose();
            client.Close();
            client.Dispose();
            Console.WriteLine("Disconnected");
            Console.ReadLine();
        }
    }
}