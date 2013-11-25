using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Configuration;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using log4net;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;
using Monaco.Extensibility.Logging;

namespace Monaco.Hosting
{
	public class DefaultHost : MarshalByRefObject, IApplicationHost
	{
		private readonly ILog _logger = LogManager.GetLogger(typeof (DefaultHost));
		private IServiceBus _bus;
		private string _configurationFile;
		private MonacoConfiguration configuration;
		private ICanConfigureEndpoint _endPointConfiguration;
		private string _endpointConfigurationType;
		private string _hostAssembly;
		private Castle.Core.Configuration.IConfiguration _hostConfiguration;
		private HostConfiguration localHostConfiguration;

		public IContainer Container { get; private set; }

		#region IApplicationHost Members

		public void Start(string assemblyName)
		{
			if (string.IsNullOrEmpty(assemblyName))
				ScanForEndpointConfiguration();
			else
			{
				_hostAssembly = assemblyName.Replace(".dll", string.Empty)
					.Replace(".exe", string.Empty);
			}

			CheckForEndpointConfiguration();

			CreateContainer();

			CreateEndpoint();

			_bus = this.Container.Resolve<IServiceBus>();

			//_bus.ConfiguredWithEndpoint(_endPointConfiguration.GetType());

			_bus.Start();

			if (string.IsNullOrEmpty(assemblyName))
				assemblyName = _hostAssembly;

			_bus.Find<ILogger>().LogInfoMessage(string.Format("Host Started - [{0}]", assemblyName));
		}

		public void Stop()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (_bus != null)
			{
				_bus.Find<ILogger>().LogInfoMessage(string.Format("Host Stopped - [{0}]", _hostAssembly));

				_bus.Stop();
			}
			_bus = null;

			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;
		}

		public void SetHostAssembly(string assemblyName)
		{
			_hostAssembly = assemblyName.Replace(".dll", string.Empty).Replace(".exe", string.Empty);
		}

		public void SetEndpoint(string endpointConfiguration)
		{
			_endpointConfigurationType = endpointConfiguration;
		}

		public void SetConfigurationFile(string configurationFile)
		{
			_configurationFile = configurationFile;
		}

		#endregion

		public void Start<TEndpointConfiguration>()
			where TEndpointConfiguration : ICanConfigureEndpoint, new()
		{
			_endPointConfiguration = new TEndpointConfiguration();
			Start(typeof (TEndpointConfiguration).Assembly.FullName);
		}

		private void ScanForEndpointConfiguration()
		{
			string[] files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, ".dll");

			foreach (string file in files)
			{
				try
				{
					Assembly asm = Assembly.LoadFile(file);

					if (asm == GetType().Assembly) continue;

					Type endpoint = (from type in asm.GetTypes()
					                 where typeof (BaseEndpointConfiguration).IsAssignableFrom(type)
					                       && type.IsClass && !type.IsAbstract
					                 select type).FirstOrDefault();

					if (endpoint != null)
					{
						_hostAssembly = endpoint.Assembly.GetName().Name;
						break;
					}
				}
				catch
				{
					continue;
					throw;
				}
			}
		}

		public void Configure(Func<HostConfiguration, HostConfiguration> configuration)
		{
			this.localHostConfiguration = new HostConfiguration();
			_hostConfiguration = configuration(this.localHostConfiguration).Build();
		}

		private void CreateEndpoint()
		{
			if (!string.IsNullOrEmpty(_endpointConfigurationType))
			{
				Assembly asm = Assembly.Load(_hostAssembly);

				if (asm != null)
				{
					_endPointConfiguration = asm.CreateInstance(_endpointConfigurationType)
					                         as ICanConfigureEndpoint;
				}
			}
		}

		private void CreateContainer()
		{
			// need to create the container and if an endpoint is specified, run the endpoint 
			// configuration for that specified endpoint, else proceed as normal with container initialization:
			if (Container == null && _hostConfiguration == null)
			{
				if (string.IsNullOrEmpty(_configurationFile) == false)
				{
					if (File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
					{
						if (this._endPointConfiguration != null)
						{
							configuration = new MonacoConfiguration(this._endPointConfiguration, new XmlInterpreter());
						}
						else
						{
							configuration = new MonacoConfiguration(new XmlInterpreter());
						}
					}
				}
				else
				{
					if (this._endPointConfiguration != null)
					{
						configuration = new MonacoConfiguration(this._configurationFile, this._endPointConfiguration);
					}
					else
					{
						configuration = new MonacoConfiguration(_configurationFile);
					}
				}
			}

			// build the container from the custom defined bus configuration:
			if (_hostConfiguration != null && Container == null)
			{
				configuration = new MonacoConfiguration();

				configuration.Kernel.ConfigurationStore
					.AddFacilityConfiguration(MonacoFacility.FACILITY_ID, _hostConfiguration);

				configuration.Kernel.AddFacility(MonacoFacility.FACILITY_ID, 
					new MonacoFacility());
			}

			this.Container = this.configuration.Container;
		}

		private void CheckForEndpointConfiguration()
		{
			if (_endpointConfigurationType != null)
			{
				CreateEndpoint();
				return;
			}

			Assembly target = null;

			try
			{
				target = Assembly.Load(_hostAssembly);
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				_logger.Error("The following assembly " + _hostAssembly + " could not be found. Reason: " + fileNotFoundException);
				throw;
			}
			catch (FileLoadException fileLoadException)
			{
				_logger.Error("The following assembly " + _hostAssembly + " could not be loaded. Reason: " + fileLoadException);
				throw;
			}

			List<Type> endpointConfiguration = (from type in target.GetTypes()
			                                    where type.IsClass && !type.IsAbstract
													  && typeof(ICanConfigureEndpoint).IsAssignableFrom(type)
			                                    select type).Distinct().ToList();

			// no endpoint configurations found, can not configure remote bus instance:
			if (endpointConfiguration.Count == 0)
			{
				throw new Exception("There is not a service endpoint configuration registered for the assembly " + _hostAssembly +
									". Please create a class derived from " + typeof(ICanConfigureEndpoint).FullName +
				                    " to define the endpoint configuration of your service.");
			}

			// too many endpoint configurations found, need to specifiy:
			if (endpointConfiguration.Count > 1)
			{
				throw new Exception("There are more than one service endpoint configuration registered for the assembly " +
				                    _hostAssembly +
				                    ". Please only use a single class derived from " + typeof (ICanConfigureEndpoint).FullName +
				                    " to define the endpoint configuration of your service.");
			}

			_endpointConfigurationType = endpointConfiguration[0].FullName;
		}
	}
}