# NoIPChat
**Not only IP Chat**

Extensible .NET chat server and client that can easly use many differenet transport layers. In current stage working over TCP, <a href="https://github.com/markqvist/Reticulum">Reticulum</a> in the near future.

It uses a very efficient bynary protocol that provides user authentication (WIP), sending message to multiple recipents and routing messages to other know servers.

Multi-hop routing is fully operational as of latest update.

It works without DNS available, but still requires TCP/IP.

As of latest update, Password, Msg and Data fileds in message are encrypted using AES 256, derived from ECDH on curve 25519.

Server can listen on multiple interfaces (different IP address and/or port), use IPV4 and/or IPV6, store and forward messages.

By utilising an efficient asyncronious IO it can handle thousands of clients at the same time (connected and communicating to each other, when mesages aren't saved).

**Messages storage**

Messages for users are stored in files inside Data folder with filename containing '@'.

Messages for remote servers are stored inside Data folder with filename without '@'.

**Multi hop routing explained:**
1) server A receives message that has to be delivered to unknown server B
2) server A sends message to all it's know servers
steps 1) and 2) repeat untill either it's delivered to server B, or max number of hops is reached
Max number of hops can be set for every server, currently it's 10.

**How to use?**
**Client**
1) Inside Servers.json file add server(s) you will be using
Here is an example:

[
  {
    "Name": "Server1",
    "IP": "127.0.0.1",
    "Port": 10001
  },
  {
    "Name": "Server2",
    "IP": "127.0.0.1",
    "Port": 10002
  }
]

2) Start client
3) Enter username and password, choose server and connect
4) You're ready to go

**Server**
1) Inside Servers.json add known server(s)

Here is an example:

[
  {
    "Name": "Server1",
    "LocalIP": "127.0.0.1",
    "RemoteIP": "127.0.0.1",
    "RemotePort": 10001,
    "TimeOut":  15
  },
  {
    "Name": "Server2",
    "LocalIP": "127.0.0.1",
    "RemoteIP": "127.0.0.1",
    "RemotePort": 10002,
    "TimeOut":  15
  }
]

LocalIP is IP assigned to the network interface that will be used in order to connect to the remote server.

RemoteIL is IP address of that server.

RemotePort is a port used by remote server.

TimeOut indicates number of seconds after which server to server connection will be disconnected in case of no activity (set to 0 if you want permanent connection).

2) Setup Config.xml

Name is name of this server.

Inside Interfaces you can eneter as many interfaces as you want

InterfaceIP is IP address assigned to network interface (in case of NAT, this is private IP)

IP is IP address that is used by clients and remote servers to connect (in case of NAT, this is public IP)

Port is a port number (port should be opend in firewall)

Logfile is path to Server log file. Default is Server.log

Remote is reserver for future uses and can be deleted

3) Start the server
4) ECDH key will be generated automatically on the first run

If you wish to use new key for running server after the setup, just delete Key.bin file and key will be regenerated when server is started again

**Automatic reconnection**

If connection between client and server or two servers is lost, it'll automatically try to reconnect in intervals starting from 15 ms, up to 60 s with each attempt interval is doubled.

**Logging errors**

Client errors are logged into the Client.log file in the same directory where your Client is located, choosing location for log file (WIP).

Server errors are logged into the file you set in Config.xml

**Guide**

You can use Guide to help you with installation and configuration of NoIPChat. Currently supports only configuration of Server.

**Dynamic software update**

In order to update Server dynamically, you need path to update packgage.

Once ready, type update into Server. 

You'll see messaage like the following:

Server closed

Unload success: True


Once asked, provide path to update package and press Enter key. Server will be updated automatically.
It's still possible that some updates will require restaring server, which can be done by using update-force command.

**Patching**

There are some small updates that can be done by just replacing certain parts of Server or Client itself and are used for transition period untill next update.

These updates are published as patches that are loaded the same way as plugins.

In order to patch Server or Client, you need path to patch package.

On Server just type patch and you'll be asked for the path to patch package and press Enter.

On Client click on Settings in main menu and than Patch.
You'll be asked to select Patch packet.

Once you update the Server or Client, all patches are deleted.

**Network update**

In order to update Client via network, click on Settings in main menu and than Request update from Server.

Once you get message that new version is received, click Load update in Settings>Update.

If you receive patch from the server, Client will be patched automatically.