# NoIPChat
**Not only IP Chat**

Extensible .NET chat server and client that can easly use many differenet transport layers. In current stage working over TCP, <a href="https://github.com/markqvist/Reticulum">Reticulum</a> in the near future.

It uses a very efficient bynary protocol that provides user authentication (WIP), sending message to multiple recipents and routing messages to other know servers.
Multi-hop routing (WIP).

It works without DNS available, but still requires TCP/IP.

In current state it doesn't implement any encryption anad as such SHOULDN'T BE DEPLOYED ON NON-SECURE NETWORKS. Encryption will be available soon.

Server can listen on multiple interfaces (different IP address and/or port), use IPV4 and/or IPV6, store and forward messages (1 hop only at the moment).

By utilising an efficient asyncronious IO it can handle thousands of clients at the same time (connected and communicating to each other, when mesages aren't saved).
As message storage at the moment takes place in RAM, it's primary used for storing for short amount of time, for example during network failures.