using System;
using System.IO;
using System.Reflection;
using System.Text;
using Monaco.Bus.Internals;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;

namespace Monaco.Hosting
{
	public class RemoteDomainHost : IStartable
	{
		private readonly string _applicationLocation;
		private readonly string _assemblyFileName;
		private readonly Type _endpointConfiguration;
		private readonly string hostAsm = typeof (DefaultHost).Assembly.FullName;
		private readonly string hostType = typeof (DefaultHost).FullName;
		private string _configurationFile;
		private HostedService _service;

		public RemoteDomainHost(string assembly, string configurationFile)
		{
			Assembly asm = Assembly.Load(assembly);
			_configurationFile = configurationFile;
			_applicationLocation = Path.GetDirectoryName(asm.Location);
			_assemblyFileName = Path.GetFileNameWithoutExtension(asm.Location);
		}

		public RemoteDomainHost(Type endpointConfiguration)
			: this(endpointConfiguration, null)
		{
		}

		public RemoteDomainHost(Type endPointConfiguration, string configurationFile)
		{
			if (typeof (BaseEndpointConfiguration).IsAssignableFrom(endPointConfiguration) == false)
				throw new Exception("The endpoint configuration for the remote domain host must be derivable from " +
				                    typeof (BaseEndpointConfiguration).FullName);

			_endpointConfiguration = endPointConfiguration;
			_configurationFile = configurationFile;
			_applicationLocation = Path.GetDirectoryName(endPointConfiguration.Assembly.Location);
			_assemblyFileName = Path.GetFileNameWithoutExtension(endPointConfiguration.Assembly.Location);
		}

		public RemoteDomainHost(Assembly assembly, string configurationFile)
		{
			_configurationFile = configurationFile;
			_applicationLocation = Path.GetDirectoryName(assembly.Location);
			_assemblyFileName = Path.GetFileNameWithoutExtension(assembly.Location);
		}

		public RemoteDomainHost(string applicationLocation, string assemblyFileName, string configurationFile)
		{
			_applicationLocation = applicationLocation;
			_assemblyFileName = assemblyFileName;
			_configurationFile = configurationFile;
		}

		public RemoteDomainHost(string configurationFile)
		{
			_configurationFile = configurationFile;
		}

		/// <summary>
		/// Gets the service bus instance that is started in the remote application domain.
		/// </summary>
		public HostedService Host
		{
			get { return _service; }
		}

		#region IStartable Members

		public bool IsRunning { get; private set; }

		public void Dispose()
		{
			Stop();
		}

		public void Start()
		{
			_service = CreateApplicationDomain();

			try
			{
				_service.Start();
			}
			catch (ReflectionTypeLoadException e)
			{
				var sb = new StringBuilder();
				foreach (Exception exception in e.LoaderExceptions)
				{
					sb.AppendLine(exception.ToString());
				}
				throw new TypeLoadException(sb.ToString(), e);
			}
		}

		public void Stop()
		{
			if (_service != null)
			{
				_service.Stop();
			}
			_service = null;
		}

		#endregion

		public RemoteDomainHost SetConfigurationFile(string file)
		{
			_configurationFile = file;
			return this;
		}

		private HostedService CreateApplicationDomain()
		{
			// create the app domain:
			var appDomainSetup = new AppDomainSetup
			                     	{
			                     		ApplicationBase = _applicationLocation,
			                     		ApplicationName = _assemblyFileName,
			                     		ConfigurationFile = CreateConfigurationFile(),
			                     		ShadowCopyFiles = "true",
			                     		ShadowCopyDirectories = "true"
			                     	};

			AppDomain appDomain = AppDomain.CreateDomain(_assemblyFileName, null, appDomainSetup);
			return CreateRemoteHost(appDomain);
		}

		private HostedService CreateRemoteHost(AppDomain appDomain)
		{
			object instance = appDomain.CreateInstanceAndUnwrap(hostAsm, hostType);

			var host = (IApplicationHost) instance;
			host.SetHostAssembly(_assemblyFileName);
			host.SetConfigurationFile(_configurationFile);

			if (_endpointConfiguration != null)
			{
				host.SetEndpoint(_endpointConfiguration.FullName);
			}

			return new HostedService(host, _assemblyFileName, appDomain);
		}

		private string CreateConfigurationFile()
		{
			if (_configurationFile != null)
				return _configurationFile;

			_configurationFile = Path.Combine(_applicationLocation, _assemblyFileName + ".dll.config");

			if (File.Exists(_configurationFile) == false)
				_configurationFile = Path.Combine(_applicationLocation, _assemblyFileName + ".exe.config");

			return _configurationFile;
		}
	}
}