using System;
using System.IO;

namespace UnturnedNetworkAPI.Clients
{
    static class Logger
    {
        public static void NetworkChatLog(string textline)
        {
            if (String.IsNullOrEmpty(textline))
                return;
            DateTime currentTime = DateTime.Now;
            StreamWriter streamWriter = new StreamWriter("Unturned_Data/Managed/mods/UnturnedNetworkAPI/NetworkChat.log", true);
            streamWriter.WriteLine($"({currentTime.ToString("yyyy-MM-dd HH:mm:ss")}): {textline}");
            streamWriter.Close();
        }
        public static void NetworkCommandsLog(string textline)
        {
            if (String.IsNullOrEmpty(textline))
                return;
            DateTime currentTime = DateTime.Now;
            StreamWriter streamWriter = new StreamWriter("Unturned_Data/Managed/mods/UnturnedNetworkAPI/NetworkRCON.log", true);
            streamWriter.WriteLine($"({currentTime.ToString("yyyy-MM-dd HH:mm:ss")}): {textline}");
            streamWriter.Close();
        }
    }
}
