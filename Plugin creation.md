**Plugin creation**

Server plugin is created by implementing IPlugin interface from Server_base namespace.

Client plugin is created by implementing IPlugin from Client namespace.

Inside an assembly there should be only one class that implements IPlugin.

Plugin should be compiled as self-contained in order not to require .NET installed. Once compiled, use Packer to sign and pack the plugin.

Make sure packed .nip file is inside Plugins folder. Once plugin is loaded, .nip file will be deleted.

**Official plugin list**

[NoIPChat mail](https://github.com/bornac1/NoIPChat-mail)

NoIPChat mail supports usage of email clients with NoIPChat network using SMTP and POP3 protocols.