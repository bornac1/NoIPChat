# NoIPChat
**Not only IP Chat**

Extensible .NET chat server and client that can easly use many differenet transport layers. In current stage working over TCP, <a href="https://github.com/markqvist/Reticulum">Reticulum</a> in the near future.

It uses a very efficient bynary protocol that provides user authentication (WIP), sending message to multiple recipents and routing messages to other know servers.

Multi-hop routing works only for messages. Multi-hop still can't be used for login and auth.

It works without DNS available, but still requires TCP/IP.

As of latest update, Password, Msg and Data fileds in message are encrypted using AES 256, derived from ECDH on curve 25519.

Server can listen on multiple interfaces (different IP address and/or port), use IPV4 and/or IPV6, store and forward messages (1 hop only at the moment).

By utilising an efficient asyncronious IO it can handle thousands of clients at the same time (connected and communicating to each other, when mesages aren't saved).
As message storage at the moment takes place in RAM, it's primary used for storing for short amount of time, for example during network failures.

**Multi hop routing explained:**
1) server A receives message that has to be delivered to unknown server B
2) server A sends message to all it's know servers
steps 1) and 2) repeat untill either it's delivered to server B, or max number of hops is reached
Max number of hops can be set for every server, currently it's 10.