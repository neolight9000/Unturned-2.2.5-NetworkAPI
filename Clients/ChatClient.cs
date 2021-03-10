using CommandHandler;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnturnedNetworkAPI.Clients
{
    public class ChatClient
    {
        public Socket ClientSocket { get; }
        public DateTime ConnectedAt { get; private set; }
        public bool IsAdmin { get; private set; }
        public int ClientID { get; private set; }
        public string ClientIP { get; private set; }
        public int ClientPort { get; private set; }

        private MainManager.onNewMessageDelegate msgdelegate = null;
        private NetworkPlayerDelegate onPlayerConnectedDelegate = null;
        private NetworkPlayerDelegate onPlayerDisconnectedDelegate = null;
        private DateTime lastMessageTime;
        public enum AuntificationState
        {
            Successfully, WrongPassword, Error
        }
        public ChatClient(Socket clientsock)
        {
            this.ClientSocket = clientsock;
            this.lastMessageTime = new DateTime();
            StartConnectionWithClient();
        }
        private void StartConnectionWithClient()
        {
            this.ClientIP = ((IPEndPoint)ClientSocket.RemoteEndPoint).Address.ToString();
            this.ClientPort = ((IPEndPoint)ClientSocket.RemoteEndPoint).Port;
            if (MainManager.ChatSecurePasswordConnection)
            {
                int AttemptionsCount = MainManager.AttemptionsCount(ClientIP);
                if (AttemptionsCount != -1)
                {
                    if((DateTime.Now - (MainManager.GetAttemptions(ClientIP).LastAttemption)).TotalHours < 24)
                    {
                        if((MainManager.GetAttemptions(ClientIP).Attemptions >= MainManager.MaxAttemptionsPerDay))
                        {
                            ClientSocket.Close();
                            return;
                        }
                    }
                    else
                    {
                        MainManager.DischargeAttemptions(ClientIP);
                    }
                }
                AuntificationState state;
                PasswordChecker(out state);
                switch (state)
                {
                    case AuntificationState.WrongPassword:
                        MainManager.AddAttemption(ClientIP);
                        ClientSocket.Send(Encoding.UTF8.GetBytes("Wrong Password! Enter correct password"));
                        ClientSocket.Close();
                        return;
                    case AuntificationState.Error:
                        ClientSocket.Send(Encoding.UTF8.GetBytes("Something went wrong, try again"));
                        ClientSocket.Close();
                        return;
                    case AuntificationState.Successfully:
                        ConnectedAt = DateTime.Now;
                        this.ClientID = MainManager.ChatClients.Count + 1;
                        Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Successfully connected to the Network chat. ChatClientID: {this.ClientID}");
                        ClientSocket.Send(Encoding.UTF8.GetBytes("The password is correct, you have successfully connected to the chat."));
                        MainManager.DischargeAttemptions(ClientIP);
                        break;
                }
            }
            MainManager.ChatClients.Add(this);
            msgdelegate = new MainManager.onNewMessageDelegate(TX);
            onPlayerConnectedDelegate = new NetworkPlayerDelegate(onPlayerConnectedTX);
            onPlayerDisconnectedDelegate = new NetworkPlayerDelegate(onPlayerDisconnectedTX);
            new Thread(RX).Start();
            MainManager.onMessage += msgdelegate;
            NetworkEvents.onPlayerConnected += onPlayerConnectedDelegate;
            NetworkEvents.onPlayerDisconnected += onPlayerDisconnectedDelegate;
        }

        private void onPlayerConnectedTX(NetworkPlayer networkPlayer)
        {
            new Thread(() => onPlayerConnectedTXAsync(networkPlayer)).Start();
        }
        private void onPlayerDisconnectedTX(NetworkPlayer networkPlayer)
        {
            BetterNetworkUser player = UserList.getUserFromPlayer(networkPlayer);
            if (this.IsAdmin)
            {
                ClientSocket.Send(Encoding.UTF8.GetBytes($"|2|{player.name}|{networkPlayer.ipAddress}:{networkPlayer.port}|"));
            }
            else
                ClientSocket.Send(Encoding.UTF8.GetBytes($"|2|{player.name}|"));
        }
        private void onPlayerConnectedTXAsync(NetworkPlayer networkPlayer)
        {
            Thread.Sleep(300);
            BetterNetworkUser player = UserList.getUserFromPlayer(networkPlayer);
            if (this.IsAdmin)
            {
                ClientSocket.Send(Encoding.UTF8.GetBytes($"|1|{player.name}|{networkPlayer.ipAddress}:{networkPlayer.port}|"));
            }
            else
                ClientSocket.Send(Encoding.UTF8.GetBytes($"|1|{player.name}|"));
        }
        private void PasswordChecker(out AuntificationState state)
        {
            try
            {
                byte[] buffer = new byte[ClientSocket.ReceiveBufferSize];
                ClientSocket.Receive(buffer);
                string receivedPassword = Encoding.UTF8.GetString(buffer).Split('|')[1];
                if (receivedPassword == MainManager.ClientChatPassword)
                {
                    IsAdmin = false;
                    state = AuntificationState.Successfully;
                }
                else if(receivedPassword == MainManager.AdminChatPassword)
                {
                    IsAdmin = true;
                    state = AuntificationState.Successfully;
                }
                else
                    state = AuntificationState.WrongPassword;
            }
            catch
            {
                state = AuntificationState.Error;
            }
        }
        private void RX()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (!ClientSocket.Connected)
                        {
                            Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Disconnected from the Network chat. Connection time: {(DateTime.Now - this.ConnectedAt)}. ChatClientID: {this.ClientID}");
                            MainManager.onMessage -= msgdelegate;
                            NetworkEvents.onPlayerConnected -= onPlayerConnectedDelegate;
                            NetworkEvents.onPlayerDisconnected -= onPlayerDisconnectedDelegate;
                            MainManager.ChatClients.Remove(this);
                            return;
                        }
                        byte[] buffer = new byte[ClientSocket.ReceiveBufferSize];
                        ClientSocket.Receive(buffer);
                        if (String.IsNullOrEmpty(Encoding.UTF8.GetString(buffer)))
                            continue;
                        if (lastMessageTime != null)
                        {
                            if (!this.IsAdmin)
                            {
                                if ((DateTime.Now - lastMessageTime).TotalSeconds < MainManager.ChatCooldown) //too Fast
                                {
                                    InfoSender($"You chatting too fast, send message every {MainManager.ChatCooldown} second/s");
                                    continue;
                                }
                            }
                        }
                        string[] serial = Encoding.UTF8.GetString(buffer).Split('|');
                        string sender_name = serial[1];
                        string sender_message = serial[2];
                        MainManager.SendMessagetoChatLocal(sender_name, sender_message);
                        Logger.NetworkChatLog($"(networkuser) {sender_name}: {sender_message} . ChatClientID: {this.ClientID}");
                        lastMessageTime = DateTime.Now;
                    }
                    catch{lastMessageTime = DateTime.Now; Thread.Sleep(1000); continue; }
                }
            }
            catch
            {
                try
                {
                    ClientSocket.Close();
                    Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Disconnected from the Network chat. Connection time: {(DateTime.Now - this.ConnectedAt)} . ChatClientID: {this.ClientID} ");
                }
                catch {MainManager.ChatClients.Remove(this); Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Disconnected from the Network chat. Connection time: {(DateTime.Now - this.ConnectedAt)} . ChatClientID: {this.ClientID}"); return; }
                MainManager.onMessage -= msgdelegate;
                NetworkEvents.onPlayerConnected -= onPlayerConnectedDelegate;
                NetworkEvents.onPlayerDisconnected -= onPlayerDisconnectedDelegate;
                MainManager.ChatClients.Remove(this);
                return;
            }
        }
        private void TX(Message msg)
        {
            try
            {
                if (!ClientSocket.Connected)
                {
                    Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Disconnected from the Network chat. Connection time: {(DateTime.Now - this.ConnectedAt)} . ChatClientID: {this.ClientID}");
                    MainManager.onMessage -= msgdelegate;
                    NetworkEvents.onPlayerConnected -= onPlayerConnectedDelegate;
                    NetworkEvents.onPlayerDisconnected -= onPlayerDisconnectedDelegate;
                    MainManager.ChatClients.Remove(this);
                    return;
                }
                SendMessage(msg);
            }
            catch
            {
                try
                {
                    ClientSocket.Close();
                    MainManager.onMessage -= msgdelegate;
                    NetworkEvents.onPlayerConnected -= onPlayerConnectedDelegate;
                    NetworkEvents.onPlayerDisconnected -= onPlayerDisconnectedDelegate;
                    MainManager.ChatClients.Remove(this);
                    Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Disconnected from the Network chat. Connection time: {(DateTime.Now - this.ConnectedAt)} . ChatClientID: {this.ClientID}");
                }
                catch
                {
                    MainManager.ChatClients.Remove(this);
                    MainManager.onMessage -= msgdelegate;
                    NetworkEvents.onPlayerConnected -= onPlayerConnectedDelegate;
                    NetworkEvents.onPlayerDisconnected -= onPlayerDisconnectedDelegate;
                    Logger.NetworkChatLog($"User with IP: {ClientIP}:{ClientPort}, Disconnected from the Network chat. Connection time: {(DateTime.Now - this.ConnectedAt)} . ChatClientID: {this.ClientID}");
                }
            }
        }
        public void InfoSender(string info)
        {
            ClientSocket.Send(Encoding.UTF8.GetBytes($"|info|{info}|"));
        }
        public void SendMessage(Message msg)
        {
            ClientSocket.Send(Encoding.UTF8.GetBytes($"|0|{msg.name}|{msg.message}|"));
        }
    }
}
