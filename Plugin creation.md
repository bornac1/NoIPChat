**Plugin creation**

Server plugin is created by implementing IPlugin interface from Server_base namespace.

Client plugin is created by implementing IPlugin from Client namespace.

Inside an assembly there should be only one class that implements IPlugin.

Plugin should be compiled as self-contained in order not to require .NET installed. Once compiled, use Packer to sign the plugin.

All plugin files should be in the same folder. This folder should be placed inside Plugins folder.
Make sure there is sign file inside the folder, or plugin won't be loaded.