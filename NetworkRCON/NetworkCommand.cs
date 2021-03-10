using System;
using System.Collections.Generic;

namespace UnturnedNetworkAPI.NetworkRCON
{
    public class NetworkCommand
    {
		private NetworkCommandDelegate _commandDelegate;
		public List<string> Names { get; protected set; }
		public int permission { get; protected set; }

		public NetworkCommandDelegate CommandDelegate
		{
			get
			{
				return _commandDelegate;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				_commandDelegate = value;
			}
		}
		public NetworkCommand(int permissionLevelRequired, NetworkCommandDelegate method, params string[] names)
		{
			this.Names = new List<string>(names);
			this.Names = this.Names.ConvertAll((string d) => d.ToLower());
			this.CommandDelegate = method;
			this.permission = permissionLevelRequired;
		}
	}
}
