**Plugin creation**

Server plugin is created by implementing IPlugin interface from Server_base.

Inside an assembly there should be only one class that implements IPlugin.

All plugin files should be in the same folder. This folder should be placed inside Plugins folder.

Plugin should be compiled as self-contained in order not to require .NET installed.