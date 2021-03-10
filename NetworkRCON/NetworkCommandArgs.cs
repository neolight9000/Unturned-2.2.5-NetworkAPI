using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnturnedNetworkAPI.Clients;

namespace UnturnedNetworkAPI.NetworkRCON
{
    public class NetworkCommandArgs
    {
		public string CommandString { get; private set; }
		public NetworkAccount senderAccount { get; private set; }
		public CommandsClient sender { get; private set; }
		public List<string> Parameters { get; private set; }

		public string ParametersAsString
		{
			get
			{
				string text = String.Empty;
				foreach (string parameter in Parameters)
				{
					text = text + parameter + " ";
				}
				return text.TrimEnd(' ');
			}
		}
		public NetworkCommandArgs(string CommandName, NetworkAccount senderAccount, CommandsClient sender, List<string> args)
		{
			this.CommandString = CommandName;
			this.senderAccount = senderAccount;
			this.sender = sender;
			this.Parameters = args;

		}
	}
}
