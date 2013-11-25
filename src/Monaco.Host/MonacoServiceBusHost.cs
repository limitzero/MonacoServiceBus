using System;
using System.Collections.Specialized;
using System.Reflection;
using System.ServiceProcess;
using System.IO;
using Monaco.Hosting;

namespace Monaco.Host
{
	public class MonacoServiceBusHost : ServiceBase
	{
		private RemoteAppDomainHost _host;
		private IServiceBus _bus;
		private string _assemblyName;
		private string _configurationFile;
		private string _endpointConfiguration;

		protected override void OnStart(string[] args)
		{
			base.OnStart(args);

			try
			{
				Assembly assembly = null;
				string serviceName = string.Empty;

				UtilClass.ScanForEndpointConfiguration(ref serviceName, ref assembly);

				if (string.IsNullOrEmpty(this._configurationFile))
					this._configurationFile = string.Concat(assembly.GetName().Name, ".config");

				var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this._configurationFile);

				if(!File.Exists(configFile))
					throw new FileNotFoundException("The configuration file could not be found in the executable directory for the service host. " + 
					"Please create the configuration file as the following {name of the assembly}.config in order for the host to include it for configuring the bus.", 
					configFile);

				System.Console.Title = string.Format("Monaco Service Bus Host: Service '<{0}>')", assembly.GetName().Name);

				_host = new RemoteAppDomainHost();
				_host.ConfigureWith(c => c.HostAssemblyDirectoryLocationOf(AppDomain.CurrentDomain.BaseDirectory)
				                         	.HostAssemblyNameOf(assembly.GetName().Name)
				                         	.ConfigurationFileNameOf(this._configurationFile));
					
				_host.Start();
			}
			catch (Exception exception)
			{
				Console.WriteLine(string.Format("Error starting host for message bus: {0}", exception.ToString()));
				Console.ReadKey();
			}

		}

		protected override void OnStop()
		{
			base.OnStop();

			if (_bus != null)
			{
				_bus.Stop();
			}
			_bus = null;

			if (_host != null)
			{
				_host.Dispose();
				_host = null;
			}

			Environment.Exit(0);
		}

		public void SetArguements(StringDictionary commands)
		{
			if (commands.ContainsKey(CommandOptions.Assembly.ToString().ToLowerInvariant()))
				this._assemblyName = commands[CommandOptions.Assembly.ToString().ToLowerInvariant()];

			if (commands.ContainsKey(CommandOptions.Configuration.ToString().ToLowerInvariant()))
				this._configurationFile = commands[CommandOptions.Configuration.ToString().ToLowerInvariant()];

			if (commands.ContainsKey(CommandOptions.Endpoint.ToString().ToLowerInvariant()))
				this._endpointConfiguration = commands[CommandOptions.Endpoint.ToString().ToLowerInvariant()];
		}

		public void Start(string[] arguements)
		{
			OnStart(arguements);
		}

		private static Exception HostedAssemblyFileNameNotSpecifiedException()
		{
			return new ArgumentException("The file name of the assembly containing the hosted components was not specified on the command line. ");
		}
	}
}