using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace Monaco.Host.Modes
{
	public class InstallMode : IRunMode
	{
		public void Execute(StringDictionary commands)
		{
			Assembly assembly = null;
			string discoveredServiceName = string.Empty; 

			//var assembly = commands[CommandOptions.Assembly.ToString().ToLower()];
			var serviceName = commands[CommandOptions.Service.ToString().ToLower()];
			var serviceDescription = commands[CommandOptions.Description.ToString().ToLower()];

			UtilClass.ScanForEndpointConfiguration(ref discoveredServiceName, ref assembly);

			HostServiceInstaller installer = new HostServiceInstaller();

			var name = serviceName ?? discoveredServiceName;
			var description = serviceDescription ?? discoveredServiceName;

			installer.Setup(name, description);
			installer.Install(new Hashtable());
		}
	}
}