using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnturnedNetworkAPI.NetworkRCON
{
    public class NetworkCommandsList
    {
		public static List<NetworkCommand> commands = new List<NetworkCommand>();

		public static void add(NetworkCommand command)
		{
			commands.Add(command);
		}
		public static void add(NetworkCommandDelegate method, int permissionLvl, params string[] names)
		{
			commands.Add(new NetworkCommand(permissionLvl, method, names));
		}
	}
}
