using System;
using System.Collections.Generic;
using UnturnedNetworkAPI.Clients;

namespace UnturnedNetworkAPI.NetworkRCON
{
    public static class NetworkCommandHandler
    {
		public static bool ExecuteCommand(string alias, NetworkAccount senderAccount, CommandsClient sender, List<string> parms)
		{
			foreach (NetworkCommand command in NetworkCommandsList.commands)
			{
                if (command.Names.Contains(alias))
                {
					try
					{
						if (senderAccount.PermissionLvl < command.permission)
						{
							sender.SendMessage($"You are not allowed to use that command. For command \'{command.Names[0]}\' you need lvl {command.permission}");
							return false;
						}
						command.CommandDelegate(new NetworkCommandArgs(alias, senderAccount, sender, parms));
					}
					catch (Exception ex)
					{
						sender.SendMessage($"Something went wrong while executing your command. Error: {ex.Message}");
						return false;
					}
					return true;
				}
			}
			return false;
		}
		public static bool ExecuteCommand(string alias, NetworkAccount senderAccount, CommandsClient sender)
		{
			foreach (NetworkCommand command in NetworkCommandsList.commands)
			{
				if (command.Names.Contains(alias))
				{
					try
					{
						if(senderAccount.PermissionLvl < command.permission)
                        {
							sender.SendMessage($"You are not allowed to use that command. For command \'{command.Names[0]}\' you need lvl {command.permission}");
							return false;
                        }
						command.CommandDelegate(new NetworkCommandArgs(alias, senderAccount, sender, null));
					}
					catch (Exception ex)
					{
						sender.SendMessage($"Something went wrong while executing your command. Error: {ex.Message}");
						return false;
					}
					return true;
				}
			}
			sender.SendMessage($"Error, Command \'{alias}\' was not found ;(");
			return false;
		}

	}
}
