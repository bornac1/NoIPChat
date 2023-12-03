using Messages;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Test_client
{
    public class Client
    {
        public float CV = 1;
        public bool? auth;
        public bool connected = false;
        public ConcurrentQueue<Messages.Message> messages;
        public string? Username;
        public string? Password;
        public string? Server;
        public TcpClient client;
        public NetworkStream? stream;
        private readonly Processing processing;
        private readonly byte[] buffer = new byte[1024];
        private int bytesRead;
        private int bufferOffset;
        public ConcurrentQueue<Messages.Message> messages_rec;
        private readonly StringBuilder value;
        public bool ischatready = false;
        private bool disconnectstarted;
        public Messages.Message message;
        public Client()
        {
            disconnectstarted = false;
            processing = new Processing();
            messages = [];
            messages_rec = [];
            value = new StringBuilder();
            client = new TcpClient();
            message = new Messages.Message();
        }
        public async Task Connect()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(IPAddress.Parse("192.168.114.169"), 10001);
                stream = client.GetStream();
                connected = true;
            }
            catch (Exception ex)
            {
                if (ex is SocketException)
                {
                    //Error connecting
                    await Disconnect();
                }
                else
                {
                    //Clean all
                    await Disconnect();
                    //Logging
                }
            }
        }
        private async Task Receive()
        {
            while (connected)
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
                                Messages.Message message = await processing.Deserialize(messageBytes);
                                await ProcessMessage(message);
                            }
                            catch (Exception ex)
                            {
                                //Can't be fixed
                                //There is error with the message
                                //Just give up
                            }
                        }
                    }

                    // Read more bytes from the stream
                    if (stream != null)
                    {
                        int bytesReadNow = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);

                        // Check if the stream has reached its end
                        if (bytesReadNow == 0)
                        {
                            connected = false;
                            break;
                        }

                        bytesRead += bytesReadNow;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is IOException)
                    {
                        //assume disconnection
                        await Disconnect();
                    }
                    else
                    {
                        //Logging
                        //Clean all
                        await Disconnect();
                    }
                }
            }
        }
        public async Task Login(string username, string password)
        {
            Username = username.ToLower();
            Password = password;
            message.CV = CV;
            message.User = Username;
            message.Pass = Password;
            if (await SendMessage(message))
            {
                _ = Receive();
            }
        }
        public async Task<bool> SendMessage(Messages.Message message)
        {
            bool msgerror = false;
            try
            {
                byte[]? data = await processing.Serialize(message);
                if (data != null)
                {
                    byte[] length = BitConverter.GetBytes(data.Length);
                    if (stream != null)
                    {
                        await stream.WriteAsync(length);
                        await stream.WriteAsync(data);
                        return true;
                    }
                    return false;
                }
                else
                {
                    //Message error
                    msgerror = true;
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException)
                {
                    //Assume disconnection
                    await Disconnect();
                    //Save message to be sent later
                    if (!msgerror)
                    {
                        messages.Enqueue(message);
                    }
                    else
                    {
                        //Message error
                        //Give up
                    }
                }
                else
                {
                    //Logging
                }
            }
            return false;
        }
        private async Task ProcessMessage(Messages.Message message)
        {
            if (message.Auth == true)
            {
                //User is authenticated
                auth = true;
            }
            else if (message.Auth == false)
            {
                auth = false;
            }
            else if (message.Msg != null || message.Data != null)
            {
                await PrintMessage(message);
            }
        }
        public async Task PrintMessage(Messages.Message message)
        {
            Console.WriteLine(message.Msg);
        }
        public async Task Disconnect(bool force = false)
        {
            if (!disconnectstarted)
            {
                disconnectstarted = true;
                try
                {
                    connected = false;
                    if (stream != null)
                    {
                        await stream.FlushAsync();
                        stream.Close();
                        await stream.DisposeAsync();
                    }
                    if (client != null)
                    {
                        client.Close();
                        client.Dispose();
                    }
                    await processing.Close();
                }
                catch (Exception ex)
                {
                    //Logging
                }
            }
        }
        public static void Main()
        {
            for (int i = 0; i<100; i++)
            {
                Thread T = new Thread(new ThreadStart(async () => {
                    var c = new Client();
                    await c.Connect();
                    await c.Login("client" + i + "@server1", "pass");
                    while (c.auth != true)
                    {
                        await Task.Delay(10);
                    }
                    for (int j = 0; j < 100; j++)
                    {
                        await c.SendMessage(new Message()
                        {
                            Receiver = "client"+i+"@server1",
                            Sender = "client" + i + "@server1",
                            Msg = "Test message "+j+" from client "+i
                        });
                        Thread.Sleep(1000);
                    }
                })); T.Start(); T.Join(); Thread.Sleep(10);
            }
            Console.ReadLine();
        }
    }
}