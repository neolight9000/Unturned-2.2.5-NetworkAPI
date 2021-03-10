using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UnturnedNetworkAPI.NetworkRCON
{
    class NetworkCommandHooker : MonoBehaviour
    {
		private ArrayList serverComponents = new ArrayList();

		private static GameObject previousObject;
		public void NetworkAddonsLoader()
        {
			string path = "Unturned_Data/Managed/mods/UnturnedNetworkAPI/Addons/";
			if (!Directory.Exists("Unturned_Data/Managed/mods/UnturnedNetworkAPI/Addons/"))
				Directory.CreateDirectory(path);
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			FileInfo[] files = directoryInfo.GetFiles("*.dll");
			int num = files.Length;
			string[] array = new string[num];
			for (int i = 0; i < num; i++)
			{
				string name = files[i].Name;
				array[i] = files[i].Name.Substring(0, name.Length - 4);
			}
			Log($"{num} .dll files found in {path} folder");
			for (int i = 0; i < num; i++)
			{
				Log(i + 1 + ")");
				Log("Trying to load assembly: " + array[i] + ".dll");
				Assembly assembly = null;
				try
				{
					assembly = Assembly.LoadFrom(path + array[i] + ".dll");
				}
				catch (Exception ex)
				{
					Log("Error loading addon: " + ex.Message);
					Exception innerException = ex.InnerException;
					Log(innerException.Message);
				}
				Log("Assembly loaded in, looking for classes to inject.. ");
				Type[] array2 = null;
				Type[] array3;
				try
				{
					array2 = assembly.GetTypes();
				}
				catch (Exception ex)
				{
					Log(ex.Message);
					Log(ex.GetType().ToString());
					Type[] types = ((ReflectionTypeLoadException)ex).Types;
					array3 = types;
					foreach (Type type in array3)
					{
						try
						{
							Log(type.ToString());
						}
						catch (Exception ex2)
						{
							Log(ex2.Message);
							Log("The .NET version of this addon is probably not compatible with unturned");
						}
					}
					Log(ex.Source);
					Log(ex.Data.ToString());
					Log(ex.TargetSite.ToString());
				}
				bool flag = false;
				bool flag2 = false;
				MethodInfo methodInfo = null;
				array3 = array2;
				foreach (Type type2 in array3)
				{
					if (!type2.Name.Equals("Loader"))
					{
						continue;
					}
					flag = true;
					Log("Loader class found in " + array[i] + ".dll, looking for load() or attach() method...");
					MethodInfo[] methods = type2.GetMethods();
					MethodInfo[] array4 = methods;
					foreach (MethodInfo methodInfo2 in array4)
					{
						if (methodInfo2.Name == "Load" || methodInfo2.Name == "load" || methodInfo2.Name == "attach" || methodInfo2.Name == "Attach")
						{
							flag2 = true;
							methodInfo = methodInfo2;
							Log(methodInfo.Name + "() method found.");
							break;
						}
					}
					break;
				}
				array3 = array2;
				foreach (Type type2 in array3)
				{
					if (type2.Name.Contains("PrivateImplementation") || type2.Name.Contains("StaticArray") || type2.Name.Contains("IniFile") || type2.Name.Contains("DisplayClass"))
					{
						continue;
					}
					if (!flag)
					{
						try
						{
							Log("Found class: \"" + type2.Name + "\" in " + array[i] + ".dll");
							NetworkCommandLoader.gameobject.AddComponent(type2);
							Log("Class \"" + type2.Name + "\" added as Component to gameobject");
						    serverComponents.Add(type2.Name);
							UnityEngine.Object.DontDestroyOnLoad(NetworkCommandLoader.gameobject);
						}
						catch (Exception ex)
						{
							Log("Error loading class: " + ex.Message);
							Exception innerException = ex.InnerException;
							Log(innerException.Message);
						}
					}
					else if (type2.Name.Equals("Loader"))
					{
						if (flag2)
						{
							Log("Calling " + methodInfo.Name + "() in the Loader class.");
							try
							{
								methodInfo.Invoke(type2, null);
							}
							catch (Exception ex)
							{
								Log("Error loading class: " + ex.Message);
								Exception innerException = ex.InnerException;
								Log(innerException.Message);
							}
						}
						else
						{
							Log("No load() or attach() method found in Loader class. Will instead add the Loader class as Component to gameobject");
							try
							{
								Log("Found class: \"" + type2.Name + "\" in " + array[i] + ".dll");
								NetworkCommandLoader.gameobject.AddComponent(type2);
								UnityEngine.Object.DontDestroyOnLoad(NetworkCommandLoader.gameobject);
								Log("Class \"" + type2.Name + "\" added as Component to gameobject");
							}
							catch (Exception ex)
							{
								Log("Error loading class: " + ex.Message);
								Exception innerException = ex.InnerException;
								Log(innerException.Message);
							}
						}
					}
				}
			}
			Log("Finished loading .dll files");
			Log("---------------------------------");
			Log(" ");
		}
		public static void Log(string p)
        {
			StreamWriter streamWriter = new StreamWriter("Unturned_Data/Managed/mods/UnturnedNetworkAPI/AddonLogs.txt", true);
			streamWriter.WriteLine(p);
			streamWriter.Close();
		}
		private void Start()
		{
			NetworkAddonsLoader();
			if (previousObject == null)
			{
				previousObject = NetworkCommandLoader.gameobject;
			}
		}
	}
}
