using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnturnedNetworkAPI.NetworkRCON
{
    class NetworkCommandLoader : MonoBehaviour
    {
		public static GameObject gameobject;

		public static GameObject keepAlive;

		public static void hook()
		{
			File.WriteAllText("Unturned_Data/Managed/mods/UnturnedNetworkAPI/AddonLogs.txt", string.Empty);
			if (gameobject == null)
			{
				gameobject = getNetworkChat().gameObject;
				Object.DontDestroyOnLoad(gameobject);
				gameobject.AddComponent<NetworkCommandHooker>();
			}
			if (keepAlive == null)
			{
				keepAlive = new GameObject();
				Object.DontDestroyOnLoad(keepAlive);
				keepAlive.AddComponent<NetworkCommandLoader>();
			}
		}

		private void Update()
		{
			if (gameobject == null)
			{
				NetworkCommandHooker.Log("GameObject was destroyed! Finding new gameobject...");
				gameobject = getNetworkChat().gameObject;
				NetworkCommandHooker.Log("Found new GameObject :)");
				Object.DontDestroyOnLoad(gameobject);
				gameobject.AddComponent<NetworkCommandHooker>();
			}
		}
		private static NetworkChat getNetworkChat()
		{
			return Object.FindObjectOfType(typeof(NetworkChat)) as NetworkChat;
		}
	}
}
