using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnturnedNetworkAPI.NetworkRCON;

namespace UnturnedNetworkAPI.Clients
{
    public class CommandsClient
    {
        public NetworkAccount Account { get; private set; }
        public enum AuntificationState
        {
            Successfully, WrongPassword, Error
        }
        public DateTime LastCommandTime { get; private set; }
        public DateTime ConnectedAt { get; private set; }
        public string LastUsedCommand { get; private set; }
        public int ClientID { get; private set; }
        public string ClientIP { get; private set; }
        public int ClientPort { get; private set; }
        public bool Connected => ClientSocket.Connected;
        public IPEndPoint EndPoint => (IPEndPoint)ClientSocket.RemoteEndPoint;
        private Socket ClientSocket { get; }
        public CommandsClient(Socket client)
        {
            this.ClientSocket = client;
            LastCommandTime = new DateTime();
            StartConnectionWithClient();
        }
        private void StartConnectionWithClient()
        {
            this.ClientIP = EndPoint.Address.ToString();
            this.ClientPort = EndPoint.Port;
            int AttemptionsCount = MainManager.AttemptionsCount(ClientIP);
            if (AttemptionsCount != -1)
            {
                if ((DateTime.Now - (MainManager.GetAttemptions(ClientIP).LastAttemption)).TotalHours < 24)
                {
                    if ((MainManager.GetAttemptions(ClientIP).Attemptions >= MainManager.MaxAttemptionsPerDay))
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
            NetworkAccount acc = Login(out state);
            this.Account = acc;
            switch (state)
            {
                case AuntificationState.WrongPassword:
                    MainManager.AddAttemption(ClientIP);
                    SendMessage("Wrong Password! Enter correct password");
                    return;
                case AuntificationState.Error:
                    SendMessage("Something went wrong, try again");
                    return;
                case AuntificationState.Successfully:
                    this.ClientID = MainManager.CommandsClients.Count + 1;
                    ConnectedAt = DateTime.Now;
                    SendMessage("The password is correct, you have successfully entered to the RCON");
                    Logger.NetworkCommandsLog($"User with IP: {ClientIP}:{ClientPort}, Login: {this.Account.Login}, Connected to the RCON. ClientID {this.ClientID}");
                    MainManager.DischargeAttemptions(ClientIP);
                    break;
            }
            MainManager.CommandsClients.Add(this);
            new Thread(CommandsChecker).Start();
        }
        private NetworkAccount Login(out AuntificationState state)
        {
            try
            {
                byte[] buffer = new byte[ClientSocket.ReceiveBufferSize];
                ClientSocket.Receive(buffer);
                string[] serial = Encoding.UTF8.GetString(buffer).Split('|');
                string login = serial[1];
                string password = serial[2];
                foreach (NetworkAccount account in MainManager.Accounts)
                {
                    if(account.Login == login && account.Password == password)
                    {
                        state = AuntificationState.Successfully;
                        return account;
                    }
                }
                state = AuntificationState.WrongPassword;
                return new NetworkAccount();
            }
            catch
            {
                state = AuntificationState.Error;
                return new NetworkAccount();
            }
        }
        private void CommandsChecker()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (!ClientSocket.Connected)
                        {
                            Logger.NetworkCommandsLog($"User with IP: {ClientIP}:{ClientPort}, Login: {Account.Login}, Disconnected from RCON. Connection time: {(DateTime.Now - this.ConnectedAt)}. ClientID: {this.ClientID}");
                            return;
                        }
                        byte[] array = new byte[ClientSocket.ReceiveBufferSize];
                        ClientSocket.Receive(array);
                        if (LastCommandTime != null)
                        {
                            if (this.Account.PermissionLvl < 6)
                            {
                                if ((DateTime.Now - LastCommandTime).TotalSeconds < MainManager.CommandsCooldown) //too Fast
                                {
                                    SendMessage($"You use commands too fast, use command every {MainManager.CommandsCooldown} second/s");
                                    continue;
                                }
                            }
                        }
                        string serial = Encoding.UTF8.GetString(array);
                        string[] serial_pkg = serial.Split('|');
                        string commandName = serial_pkg[1];
                        List<string> parameters = serial_pkg.ToList<string>();
                        parameters.Remove(commandName);
                        string paramsAsString = String.Empty;
                        foreach(string parameter in parameters.ToList())
                        {
                            if (string.IsNullOrEmpty(parameter))
                                parameters.Remove(parameter);
                            else
                            {
                                paramsAsString += parameter;
                                paramsAsString += ",";
                            }
                        }
                        LastCommandTime = DateTime.Now;
                        if (parameters.Count < 1)
                        {
                            if(NetworkCommandHandler.ExecuteCommand(commandName, Account, this))
                                Logger.NetworkCommandsLog($"{Account.Login} used command \'{commandName}\', parameters: {paramsAsString}, ClientID: {ClientID}");
                        }
                        else
                        {
                            if(NetworkCommandHandler.ExecuteCommand(commandName, Account, this, parameters))
                                Logger.NetworkCommandsLog($"{Account.Login} used command \'{commandName}\', parameters: {paramsAsString}, ClientID: {ClientID}");
                        }
                    }
                    catch{LastCommandTime = DateTime.Now; Thread.Sleep(1000); continue; }
                }
            }
            catch
            {
                try
                {
                    ClientSocket.Close();
                    Logger.NetworkCommandsLog($"User with IP: {ClientIP}:{ClientPort}, Login: {Account.Login}, Disconnected from RCON. Connection time: {(DateTime.Now - this.ConnectedAt)}. ClientID: {this.ClientID}");
                }
                catch {MainManager.CommandsClients.Remove(this); Logger.NetworkCommandsLog($"User with IP: {ClientIP}:{ClientPort}, Login: {Account.Login}, Disconnected from RCON. Connection time: {(DateTime.Now - this.ConnectedAt)}. ClientID: {this.ClientID}"); return; }
                MainManager.CommandsClients.Remove(this);
                return;
            }
        }
        public void SendMessage(string msg)
        {
            ClientSocket.Send(Encoding.UTF8.GetBytes(msg));
        }
        public void InfoSender(string info)
        {
            ClientSocket.Send(Encoding.UTF8.GetBytes($"|info|{info}|"));
        }
        public void SendBytes(byte[] buffer)
        {
            try
            {
                this.ClientSocket.Send(buffer);
            }
            catch {
                try
                {
                    this.ClientSocket.Close();
                    MainManager.CommandsClients.Remove(this);
                    return;
                }
                catch { MainManager.CommandsClients.Remove(this); return; }
            }
        }

    }
}
