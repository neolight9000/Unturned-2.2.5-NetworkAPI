using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using CommandHandler;
using UnityEngine;
using System.IO;
using Ini;
using UnturnedNetworkAPI.Clients;
using UnturnedNetworkAPI.NetworkRCON;

namespace UnturnedNetworkAPI
{
    public struct Message
    {
        public string message { get; set; }
        public string name { get; set; }
        public Message(string name, string message)
        {
            this.name = name;
            this.message = message;
        }
    }
    public struct NetworkAccount
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public int PermissionLvl { get; set; }
        public NetworkAccount(string Login, string Password, int PermissionLvl)
        {
            this.Login = Login;
            this.Password = Password;
            this.PermissionLvl = PermissionLvl;
        }
    }
    public class LoginAttemptions
    {
        public string IP { get; set; }
        public int Attemptions { get; set; }
        public DateTime LastAttemption { get; set; }
        public LoginAttemptions(){}
        public LoginAttemptions(string IP, DateTime LastAttemption, int Attemptions = 0)
        {
            this.IP = IP;
            this.Attemptions = Attemptions;
            this.LastAttemption = LastAttemption;
        }
    }
    class MainManager : MonoBehaviour
    {
        public delegate void onNewMessageDelegate(Message msg);

        public static event onNewMessageDelegate onMessage;
        public static List<ChatClient> ChatClients { get; private set; }
        public static List<CommandsClient> CommandsClients { get; private set; }
        public static List<NetworkAccount> Accounts { get; set; }
        public static List<LoginAttemptions> LoginAttemptions { get; set; }
        public static int MaxAttemptionsPerDay { get; set; }
        public static int ChatMaxConnections { get; private set; } = 10;
        public static int ChatPort { get; private set; } = 4300;
        public static bool ChatEnabled { get; private set; } = true;
        public static bool ChatSecurePasswordConnection { get; private set; } = true;
        public static string ClientChatPassword { get; set; }
        public static string AdminChatPassword { get; set; }
        public static int CommandsMaxConnections { get; private set; } = 50;
        public static int CommandsPort { get; private set; } = 2000;
        public static bool CommandsEnabled { get; private set; } = true;
        public static int ChatCooldown { get; private set; } = 2;
        public static int CommandsCooldown { get; private set; } = 2;

        private string LastMessage = String.Empty;
        private string LastMessageUser = String.Empty;
        private Socket chatlistener = null;
        private Socket commandslistener = null;
        private NetworkChat networkChat = UnityEngine.Object.FindObjectOfType(typeof(NetworkChat)) as NetworkChat;
        private FieldInfo[] networkChatfields = typeof(NetworkChat).GetFields();
        public void Start()
        {
            Accounts = new List<NetworkAccount>();
            LoginAttemptions = new List<LoginAttemptions>();
            LoadConfigs();
            LoadNetworkAccounts();
            AntiBruteForceLoader();
            if(ChatEnabled)
               LoadNetworkChat();
            if (CommandsEnabled)
                LoadNetworkCommands();
            CommandList.add(new Command(8, new CommandDelegate(GetChatConnections), new string[] { "getchatconnections", "getchatcon" }));
            CommandList.add(new Command(8, new CommandDelegate(GetRCONConnections), new string[] { "getrconconnections", "getrconcon", "getcommandscon" }));
            CommandList.add(new Command(8, new CommandDelegate(GetNetworkAddonsCount), new string[] { "getaddons"}));
            CommandList.add(new Command(8, new CommandDelegate(GetAttemptionsCount), new string[] { "getattemptions" }));
            CommandList.add(new Command(8, new CommandDelegate(GetNetworkCommands), new string[] { "GetNetworkCommands" }));
            
            NetworkEvents.onPlayerConnected += new NetworkPlayerDelegate(OnPlayerConnected);
            NetworkEvents.onPlayerDisconnected += new NetworkPlayerDelegate(OnPlayerDisconnected);
            onMessage += new onNewMessageDelegate(OnNewMessage);
        }
        private void LoadConfigs()
        {
            if (!Directory.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/"))
                Directory.CreateDirectory("Unturned_Data/Managed/mods/UnturnedNetworkAPI/");
            if(!File.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/settings.ini"))
            {
                IniFile settings = new IniFile("Unturned_Data/Managed/mods/UnturnedNetworkAPI/settings.ini");
                settings.IniWriteValue("Settings", "MaxAttemptionsPerDay", "10");
            }
            if(!File.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/chatconfig.ini"))
            {
                IniFile chatconfig = new IniFile("Unturned_Data/Managed/mods/UnturnedNetworkAPI/chatconfig.ini");
                chatconfig.IniWriteValue("ChatConfig", "Chat Enabled", "true");
                chatconfig.IniWriteValue("ChatConfig", "Chat Port", "4300");
                chatconfig.IniWriteValue("ChatConfig", "Max Connections", "10");
                chatconfig.IniWriteValue("ChatConfig", "Secure Password Connection", "true");
                chatconfig.IniWriteValue("ChatConfig", "Client Password", "32pUz0KKejHQKwZAG7eQ");
                chatconfig.IniWriteValue("ChatConfig", "Admin Password", "D8rq39EE5hH5dKA8KF6Dvg3YYp8GJbjvfqeczLHD8EWxmmLzAZn4hc5SKRg2sFdu4svcBC");
                chatconfig.IniWriteValue("ChatConfig", "Network Chat Cooldown in Seconds", "2");
            }
            if (!File.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/commandsconfig.ini")){
                IniFile commandsconfig = new IniFile("Unturned_Data/Managed/mods/UnturnedNetworkAPI/commandsconfig.ini");
                commandsconfig.IniWriteValue("NetworkCommands", "Network Commands Enabled", "true");
                commandsconfig.IniWriteValue("NetworkCommands", "Network Commands Port", "2000");
                commandsconfig.IniWriteValue("NetworkCommands", "Network Commands Max Connections", "50");
                commandsconfig.IniWriteValue("NetworkCommands", "Network Commands Cooldown in Seconds", "2");
            }
            string[] chatconfs = File.ReadAllLines("Unturned_Data/Managed/mods/UnturnedNetworkAPI/chatconfig.ini");
            string[] commandsconfs = File.ReadAllLines("Unturned_Data/Managed/mods/UnturnedNetworkAPI/commandsconfig.ini");
            string[] settingsconfs = File.ReadAllLines("Unturned_Data/Managed/mods/UnturnedNetworkAPI/settings.ini");

            ChatEnabled = bool.Parse(chatconfs[1].Split('=')[1]);
            ChatPort = int.Parse(chatconfs[2].Split('=')[1]);
            ChatMaxConnections = int.Parse(chatconfs[3].Split('=')[1]);
            ChatSecurePasswordConnection = bool.Parse(chatconfs[4].Split('=')[1]);
            ClientChatPassword = chatconfs[5].Split('=')[1];
            AdminChatPassword = chatconfs[6].Split('=')[1];
            ChatCooldown = int.Parse(chatconfs[7].Split('=')[1]);

            CommandsEnabled = bool.Parse(commandsconfs[1].Split('=')[1]);
            CommandsPort = int.Parse(commandsconfs[2].Split('=')[1]);
            CommandsMaxConnections = int.Parse(commandsconfs[3].Split('=')[1]);
            CommandsCooldown = int.Parse(commandsconfs[4].Split('=')[1]);

            MaxAttemptionsPerDay = int.Parse(settingsconfs[1].Split('=')[1]);
        }
        private void LoadNetworkAccounts()
        {
            try
            {
                if (!File.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/Accounts.dat"))
                {
                    File.WriteAllText("Unturned_Data/Managed/mods/UnturnedNetworkAPI/Accounts.dat", "neolight=akYFChtu8ZCYc3LbrWCrUwNSKn9vx88vbfWsaUbQJyYNADRYnTtHxeHKr3cF36as2zKCQd9TFwphhjEJTeyFywCTVkDrwvfVxsrL=11"); // login=password=permissionLvl
                }
                string[] lines = File.ReadAllLines("Unturned_Data/Managed/mods/UnturnedNetworkAPI/Accounts.dat");
                foreach (var line in lines)
                {
                    string[] serial = line.Split('=');
                    string login = serial[0];
                    string password = serial[1];
                    int permissionLvl = int.Parse(serial[2]);
                    Accounts.Add(new NetworkAccount(login, password, permissionLvl));
                }
            }
            catch { }
        }
        private void LoadNetworkChat()
        {
            try
            {
                ChatClients = new List<ChatClient>();
                chatlistener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                chatlistener.Bind(new IPEndPoint(IPAddress.Any, ChatPort));
                chatlistener.Listen(ChatMaxConnections);
                new Thread(ChatConnectionAccepter).Start();
                new Thread(CheckNewMessage).Start();
            }
            catch{}
        }
        private void LoadNetworkCommands()
        {
            try
            {
                CommandsClients = new List<CommandsClient>();
                commandslistener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                commandslistener.Bind(new IPEndPoint(IPAddress.Any, CommandsPort));
                commandslistener.Listen(CommandsMaxConnections);
                new Thread(CommandsConnectionAccepter).Start();
            }
            catch{}
        }
        private void ChatLogger(Message msg)
        {
            Logger.NetworkChatLog($"(local) {msg.name}: {msg.message}");
        }
        private void OnNewMessage(Message msg)
        {
            Logger.NetworkChatLog($"(local) {msg.name}: {msg.message}");
        }
        private void OnPlayerConnected(NetworkPlayer player)
        {
            new Thread(() => OnPlayerConnectedAsync(player)).Start();
        }
        private void OnPlayerDisconnected(NetworkPlayer networkplayer)
        {
            BetterNetworkUser player = UserList.getUserFromPlayer(networkplayer);
            Logger.NetworkChatLog($"(local) IP: {networkplayer.ipAddress}:{networkplayer.port}, {player.name} Disconnected");
        }
        private void OnPlayerConnectedAsync(NetworkPlayer networkplayer)
        {
            Thread.Sleep(300);
            BetterNetworkUser player = UserList.getUserFromPlayer(networkplayer);
            Logger.NetworkChatLog($"(local) IP: {networkplayer.ipAddress}:{networkplayer.port}, {player.name} Connected");
        }
        public static void AntiBruteForceLoader()
        {
            if (!File.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/LoginAttempts.log"))
            {
                File.WriteAllText("Unturned_Data/Managed/mods/UnturnedNetworkAPI/LoginAttempts.log", String.Empty);
                return;
            }
            string[] lines = File.ReadAllLines("Unturned_Data/Managed/mods/UnturnedNetworkAPI/LoginAttempts.log");
            foreach (string line in lines)
            {
                try
                {
                    string IP = line.Split('=')[0];
                    DateTime lastAttemptionTime = DateTime.Parse(line.Split('=')[1]);
                    int AttemptionsCount = int.Parse(line.Split('=')[2]);
                    LoginAttemptions.Add(new LoginAttemptions(IP, lastAttemptionTime, AttemptionsCount));
                }
                catch { }
            }
        }
        public static void SaveLoginAttemptions()
        {
            var sb = new StringBuilder();
            foreach (LoginAttemptions attemptions in LoginAttemptions)
            {
                sb.AppendLine($"{attemptions.IP}={attemptions.LastAttemption}={attemptions.Attemptions}");//IP=LastAttemptionTime=AttemptionsCount
            }
            File.WriteAllText("Unturned_Data/Managed/mods/UnturnedNetworkAPI/LoginAttempts.log", sb.ToString());
        }

        public static void SendMessagetoChatLocal(string name, string message, string sender="NetworkChatUser")
        {
            NetworkChat.networkChat_0.networkView.RPC("tellChat", RPCMode.All, new object[] { name, string.Empty, sender, message, 2147483647, 3, -1 });
        }
        private void ChatConnectionAccepter()
        {
            while (true)
            {
                try
                {
                    Socket client = chatlistener.Accept();
                    ChatClient newClient = new ChatClient(client);
                }
                catch{ }
            }
        }
        private void CommandsConnectionAccepter()
        {
            while (true)
            {
                try
                {
                    Socket client = commandslistener.Accept();
                    CommandsClient newClient = new CommandsClient(client);
                }
                catch{}
            }
        }
        private void CheckNewMessage()
        {
            while (true)
            {
                try
                {
                    if (getLastMessageText() != LastMessage)
                    {
                        string _getLastMessage = getLastMessageText();
                        string _getLastMessagePlayerName = getLastMessagePlayerName();
                        string _getLastMessageFriend = getLastMessageGroup();
                        if ((_getLastMessage != LastMessage && _getLastMessagePlayerName != "Server") && (_getLastMessageFriend != "NetworkChatUser" && _getLastMessagePlayerName != "[Server]"))
                        {
                            LastMessage = _getLastMessage;
                            onMessage(new Message(_getLastMessagePlayerName, _getLastMessage));
                        }
                    }
                }
                catch { Thread.Sleep(100); continue; }
                Thread.Sleep(100);
            }
        }
        private NetworkChat getNetworkChat()
        {
            if (networkChat == null)
            {
                networkChat = (UnityEngine.Object.FindObjectOfType(typeof(NetworkChat)) as NetworkChat);
            }
            return networkChat;
        }
        private string getNetworkChatFieldByNum(int num)
        {
            try
            {
                return networkChatfields[num].GetValue(getNetworkChat()).ToString();
            }
            catch{return String.Empty;}
        }
        private string getLastMessageText()
        {
            return getNetworkChatFieldByNum(6);
        }
        private string getLastMessagePlayerName()
        {
            return getNetworkChatFieldByNum(3);
        }
        private string getLastMessageGroup()
        {
            return getNetworkChatFieldByNum(5);
        }
        public static void AddAttemption(string IP)
        {
            for (int i = 0; i < LoginAttemptions.Count; i++)
            {
                LoginAttemptions attemptions = LoginAttemptions[i];
                if (attemptions.IP == IP)
                {
                    if ((DateTime.Now - attemptions.LastAttemption).TotalHours > 24)
                    {
                        LoginAttemptions[i].Attemptions = 1;
                        LoginAttemptions[i].LastAttemption = DateTime.Now;
                        SaveLoginAttemptions();
                        return;
                    }
                    else
                    {
                        LoginAttemptions[i].Attemptions++;
                        LoginAttemptions[i].LastAttemption = DateTime.Now;
                        SaveLoginAttemptions();
                        return;
                    }
                }
            }
            LoginAttemptions.Add(new LoginAttemptions(IP, DateTime.Now, 1));
            SaveLoginAttemptions();
        }
        public static int AttemptionsCount(string IP)
        {
            foreach(LoginAttemptions loginAttemptions in LoginAttemptions)
            {
                if (loginAttemptions.IP == IP)
                    return loginAttemptions.Attemptions;
            }
            return -1;
        }
        public static LoginAttemptions GetAttemptions(string IP)
        {
            foreach (LoginAttemptions attemptions in LoginAttemptions)
            {
                if (attemptions.IP == IP)
                    return attemptions;
            }
            return new LoginAttemptions();
        }
        public static void DischargeAttemptions(string IP)
        {
            for(int i = 0; i < LoginAttemptions.Count; i++)
            {
                if(LoginAttemptions[i].IP == IP)
                    LoginAttemptions[i].Attemptions = 0;
            }
            SaveLoginAttemptions();
        }
        private void GetNetworkCommands(CommandArgs args)
        {
            foreach(NetworkCommand command in NetworkCommandsList.commands)
            {
                Reference.Tell(args.sender.networkPlayer, $"Command: {command.Names[0]}");
            }
        }
        private void GetChatConnections(CommandArgs args)
        {
            int connectedUsersCount = 0;
            foreach(ChatClient client in ChatClients.ToArray())
            {
                if (client.ClientSocket.Connected)
                {
                    connectedUsersCount++;
                    Reference.Tell(args.sender.networkPlayer, $"{((IPEndPoint)client.ClientSocket.RemoteEndPoint).Address.ToString()}:{((IPEndPoint)client.ClientSocket.RemoteEndPoint).Port.ToString()}, time: {DateTime.Now - client.ConnectedAt}");
                }
                else
                    ChatClients.Remove(client);
            }
            Reference.Tell(args.sender.networkPlayer, $"There are {connectedUsersCount} connections.");
        }
        private void GetRCONConnections(CommandArgs args)
        {
            int connectedUsersCount = 0;
            foreach (CommandsClient client in CommandsClients.ToArray())
            {
                if (client.Connected)
                {
                    connectedUsersCount++;
                    Reference.Tell(args.sender.networkPlayer, $"{(client.EndPoint).Address.ToString()}:{(client.EndPoint).Port.ToString()}, time: {DateTime.Now - client.ConnectedAt}");
                }
                else
                    CommandsClients.Remove(client);
            }
            Reference.Tell(args.sender.networkPlayer, $"There are {connectedUsersCount} connections.");
        }
        private void GetNetworkAddonsCount(CommandArgs args)
        {
            Reference.Tell(args.sender.networkPlayer, $"Count of addons: {NetworkCommandsList.commands.Count}");
        } 
        private void GetAttemptionsCount(CommandArgs args)
        {
            foreach(LoginAttemptions attemptions in LoginAttemptions)
            {
                Reference.Tell(args.sender.networkPlayer, $"{attemptions.IP}={attemptions.Attemptions}");
            }
        }
    }
}
