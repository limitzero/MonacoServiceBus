using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace Monaco.Host.Modes
{
	public class UninstallMode : IRunMode
	{
		public void Execute(StringDictionary commands)
		{
			Assembly assembly = null;
			string discoveredServiceName = string.Empty;

			var serviceName = commands[CommandOptions.Service.ToString().ToLower()];

			UtilClass.ScanForEndpointConfiguration(ref discoveredServiceName, ref assembly);

			HostServiceInstaller installer = new HostServiceInstaller();

			var name = serviceName ?? discoveredServiceName;

			installer.Setup(name);
			installer.Uninstall(new Hashtable());
		}
	}
}