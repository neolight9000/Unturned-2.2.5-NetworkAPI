# Unturned-2.2.5-NetworkAPI
multifunctional plugin - API that allows you to create your own add-on plugins to interact through RCON. The plugin also has a built-in NetworkChat, with which you can chat over TCP / IP.

# RCON Side
This plugin using your RCON addons and any client can invoke them over TCP / IP.
RCON processes each tcp / ip client asynchronously.
RCON addons can be made using the UnturnedNetworkAPI.dll library by adding your command to the general list of commands.
Example:
```csharp
  using UnturnedNetworkAPI.NetworkRCON;
  
  NetworkCommandsList.add(new NetworkCommand(8, new NetworkCommandDelegate(SayHello), new string[] { "hello", "hellowrld" })); // adding your command to the static  Commands List.
  
  private void SayHello(NetworkCommandArgs args)
  {
			byte[] serverbuffer = Encoding.UTF8.GetBytes("Hello, World!"); // You send "Hello, World" Message to network client-caller.
			args.sender.SendBytes(serverbuffer);
  }
```csharp
You need to add your addons to the UnturnedNetworkAPI/Addons/ Folder.
You can set up cooldown to use commands for the regular player.
You can interact with the RCON network client through the CommandsClient class.

# Basic RCON Commands - Addon
We also made our own RCON Addon, which contains all the basic network commands.
You can download it in our github page.

# NetworkChat
We also added a built-in Network chat, through which, using TCP / IP Sockets, you can chat with players over TCP / IP.
The plugin listens on the port of your choice and processes each connected client asynchronously.
You can set up cooldown for chat to the regular player.

# Anti-BruteForce
The plugin has an anti-bruteforce function, with which you can insist the maximum number of unsuccessful login attempts per day. This will prevent an attacker from gaining access to your RCON or Network chat.

# NetworkChat Configurations
You can configure the network chat in the UnturnedNetworkAPI / chatconfig.ini settings file.

Chat Enabled - true/false  Enable / Disable network chat \n
Chat Port - Port Number  Use TCP port for network chat \n
Max Connections - Number  Maximum number of chat connections at one time \n
Secure Password Connection - true/false  Using a password to connect to network chat \n
Client Password - Password  Password for a regular chat user (you can leave nothing if Secure Password Connection is false) \n
Chat administrator password (you can leave nothing if Secure Password Connection is false) \n
Network Chat Cooldown in Seconds - Number  cooldown for regular user's messages \n

# RCON Configurations
You can configure the RCON settings in the UnturnedNetworkAPI / chatconfig.ini settings file.

Network Commands Enabled - true/false  Enable / Disable RCON
Network Commands Port - Port Number  Use TCP port for RCON
Network Commands Max Connections - Number  Maximum number of RCON connections at one time
Network Commands in Seconds - Number  cooldown in seconds for regular user's command

# Security Settings
You can change max. failed attempts per day in file UnturnedNetworkAPI / settings.ini settings file.

# How to write client's side code for the RCON
RCON using Socket TCP/IP. You must use this type of connection to work with RCON.
RCON accepts special data packets separated by '|'. Example: |commandname|argument1|argument2|.
RCON Works with UTF-8 encoding, you need to encode your data-packages with this, Example: Encoding.UTF8.GetBytes("|commandname|argument1|argument2|");
When connecting to RCON, the first thing you need to do is log in by sending the "|login|password|" packet. (Use UTF-8 encoding)
Example Client's side code on C#:
```csharp
      TcpClient RCONClient = new TcpClient("127.0.0.1", 4300);
			string login = "neolight";
			string password = "12245234fwasr";
			RCONClient.Client.Send(Encoding.UTF8.GetBytes($"|{login}|{password}|"));
			byte[] buffer = new byte[RCONClient.Client.ReceiveBufferSize];
			RCONClient.Client.Receive(buffer);
			Console.WriteLine($"Server's answer: {Encoding.UTF8.GetString(buffer)}");
```csharp
  
