﻿using System.Net;
using System.Net.Sockets;
using Messages;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;

namespace Client
{
    public class Client
    {
        public float CV = 1;
        public bool? auth;
        public bool connected = false;
        public ConcurrentBag<Messages.Message> messages;
        public string? Username;
        public string? Password;
        public string? Server;
        public TcpClient client;
        public NetworkStream? stream;
        private readonly Processing processing;
        private readonly byte[] buffer = new byte[1024];
        private int bytesRead;
        private int bufferOffset;
        public BindingSource servers;
        public List<Messages.Message> messages_rec;
        private readonly StringBuilder value;
        public bool ischatready = false;
        public Main main;
        private bool disconnectstarted;
        public Messages.Message message;
        public Client(Main main)
        {
            this.main = main;
            disconnectstarted = false;
            processing = new Processing();
            messages = [];
            servers = [];
            messages_rec = [];
            servers = [];
            value = new StringBuilder();
            _ = LoadServers();
            client = new TcpClient();
            message = new Messages.Message();
        }
        public async Task Connect(Servers srv)
        {
            try
                {
                    client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Parse(srv.IP), srv.Port);
                    stream = client.GetStream();
                    connected = true;
            }
            catch (Exception ex)
            {
                //Error connecting
                await Disconnect();
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
                                Console.WriteLine("Message deseialization error");
                                Console.WriteLine(ex.ToString());
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
                { //assume disconnection
                    await Disconnect();
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
                else{
                    //Message error
                    msgerror = true;
                }
            }
            catch (Exception ex)
            {
                //Assume disconnection
                await Disconnect();
                //Save message to be sent later
                if (!msgerror)
                {
                    messages.Add(message);
                }
            }
            return false;
        }
        public (string?, int) GetServer(string name)
        {
            name = name.ToLower();
            foreach(Servers server in servers)
            {
                if(server.Name == name)
                {
                    return (server.IP, server.Port);
                }
            }
            return (null, 0);
        }
        private async Task ProcessMessage(Messages.Message message)
        {
            if(message.Auth == true)
            {
                //User is authenticated
                auth = true;
            } else if (message.Auth == false)
            {
                auth = false;
            } else if(message.Msg != null || message.Data != null)
            {
                await PrintMessage(message);
            }
        }
        public async Task PrintMessage(Messages.Message message)
        {
            if (main.chat != null)
            {
                string current = main.chat.display.Text;
                string newvalue = await Task.Run(() =>
                {
                    value.Append(current);
                    value.AppendLine($"{message.Sender}:{message.Msg}");
                    string str = value.ToString();
                    value.Clear();
                    return str;
                });
                main.chat.display.Text = newvalue;
            }
            else
            {
                MessageBox.Show("Error printing message.");
            }
        }
        public async Task Disconnect(bool force= false)
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
                    //start new client
                    main.client = new Client(main);
                    await main.client.LoadServers();
                    //close all
                    main.CloseDisconnect(force);
                    main.ManipulateMenue(false);
                }
                catch (Exception ex)
                {
                    //Nothing to do
                }
            }
        }
        public async Task LoadServers()
        {
            //Load servers from Servers.json
            servers.Clear();
            string jsonString = await System.IO.File.ReadAllTextAsync("Servers.json");
            var servers_list = await Task.Run(() =>
            {
                return JsonSerializer.Deserialize<Servers[]>(jsonString);
            });
            if (servers_list != null)
                {
                foreach (Servers server in servers_list)
                    {
                        Servers srv = server;
                        srv.Name = srv.Name.ToLower();
                        //servers.TryAdd(server.Name.ToLower(), server);
                        servers.Add(srv);
                    }
                }
        }
        public async Task SaveServers()
        {
            string jsonString = await Task.Run(() =>
            {
                List<Servers> servers_list = [];
                foreach (Servers server in servers)
                {
                    /*server.Value.Name = server.Value.Name.ToLower();
                    servers_list.Add(server.Value);*/
                    servers_list.Add(server);
                }
                return JsonSerializer.Serialize(servers_list);
            });
            await System.IO.File.WriteAllTextAsync("Servers.json", jsonString);
        }
    }
}