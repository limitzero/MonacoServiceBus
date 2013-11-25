using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Monaco.Bus.Internals;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;

namespace Monaco.Hosting
{
	public class RemoteAppDomainHost : IStartable
	{
		private readonly string _hostAsm = typeof (DefaultHost).Assembly.FullName;
		private readonly string _hostType = typeof (DefaultHost).FullName;
		private HostedService _service;

		/// <summary>
		/// Gets the service instance that is started in the remote application domain.
		/// </summary>
		public HostedService Host
		{
			get { return _service; }
		}

		/// <summary>
		/// Gets the current configuration for hosting the service bus in a separate app domain.
		/// </summary>
		public RemoteAppDomainHostConfiguration Configuration { get; private set; }

		#region IStartable Members

		public bool IsRunning { get; private set; }

		public void Start()
		{
			if (IsRunning) return;

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

			IsRunning = true;
		}

		public void Stop()
		{
			if (_service != null)
			{
				_service.Dispose();
			}
			_service = null;

			IsRunning = false;
		}

		public void Dispose()
		{
			Stop();
		}

		#endregion

		/// <summary>
		///  This will configure the host that will contain the service bus instance in a separate app domain.
		/// </summary>
		/// <param name="configuration"></param>
		public RemoteAppDomainHost ConfigureWith(
			Func<RemoteAppDomainHostConfiguration, RemoteAppDomainHostConfiguration> configuration)
		{
			Configuration = configuration(new RemoteAppDomainHostConfiguration());
			return this;
		}

		private HostedService CreateApplicationDomain()
		{
			// create the app domain:
			var appDomainSetup = new AppDomainSetup
			                     	{
			                     		ApplicationBase = Configuration.HostAssemblyLocation,
			                     		ApplicationName = Configuration.HostAssembly,
			                     		ConfigurationFile = CreateConfigurationFile(),
			                     		ShadowCopyFiles = "true",
			                     		ShadowCopyDirectories = "true"
			                     	};

			AppDomain appDomain = AppDomain.CreateDomain(Configuration.HostAssembly, null, appDomainSetup);

			return CreateRemoteHost(appDomain);
		}

		private HostedService CreateRemoteHost(AppDomain appDomain)
		{
			object instance = appDomain.CreateInstanceAndUnwrap(_hostAsm, _hostType);

			var host = (IApplicationHost) instance;
			host.SetHostAssembly(Configuration.HostAssembly);
			host.SetConfigurationFile(Configuration.ConfigurationFile);

			// use the specified endpoint in the separate app domain:
			if (string.IsNullOrEmpty(Configuration.EndpointConfig) == false)
			{
				host.SetEndpoint(Configuration.EndpointConfig);
			}

			return new HostedService(host, Configuration.HostAssembly, appDomain);
		}

		private string CreateConfigurationFile()
		{
			if (Configuration.ConfigurationFile != null)
				return Configuration.ConfigurationFile;

			if (Configuration.HostAssembly == null)
				UseEndpointConfigurationAssemblyAsHostAssembly();

			string hostAssemblyName = Configuration.HostAssembly
				.Replace(".dll", string.Empty).Replace(".exe", string.Empty);

			Configuration.ConfigurationFile =
				Path.Combine(Configuration.HostAssemblyLocation, hostAssemblyName + ".dll.config");

			if (File.Exists(Configuration.ConfigurationFile) == false)
				Configuration.ConfigurationFile =
					Path.Combine(Configuration.HostAssemblyLocation, hostAssemblyName + ".exe.config");

			return Configuration.ConfigurationFile;
		}

		private void UseEndpointConfigurationAssemblyAsHostAssembly()
		{
			if (Configuration.HostAssembly == null)
			{
				string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

				foreach (string file in files)
				{
					try
					{
						Assembly asm = Assembly.LoadFile(file);
						if (asm == GetType().Assembly) continue;

						Type endpoint = (from type in asm.GetTypes()
						                 where type.IsClass && !type.IsAbstract
						                       && typeof (BaseEndpointConfiguration).IsAssignableFrom(type)
						                 select type).FirstOrDefault();

						if (endpoint != null)
						{
							Configuration.HostAssemblyNameOf(Path.GetFileName(file));
							break;
						}
					}
					catch
					{
						continue;
					}
				}
			}
		}
	}
}