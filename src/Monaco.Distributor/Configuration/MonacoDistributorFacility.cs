using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Configuration;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Monaco.Bus;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Internals.Reflection.Impl;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Configuration;
using Monaco.Configuration.Elements;
using Monaco.Distributor.Configuration.Elements;
using Monaco.Distributor.Internals.Fabric;
using Monaco.Distributor.Internals.Fabric.Impl;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Logging.Impl;
using Monaco.Extensibility.Transports;
using IConfiguration = Monaco.Configuration.IConfiguration;

namespace Monaco.Distributor.Configuration
{
	public class MonacoDistributorFacility : AbstractFacility
	{
		public readonly static string FACILITY_ID = "monaco.distributor";

		private IConfiguration configuration;
		private object[] elementBuilders = { };
		private string[] files = { };

		public IContainer GetContainer()
		{
			return this.configuration.Container;
		}

		protected override void Init()
		{
			// grab all of the files in the current run-time directory:
			this.files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

			// configure the endpoint for storage, transport and other global settings
			// also this will boot the infrastructure needed by the bus:
			this.FindBootableEndpointAndConfigure();

			// find all of the element builders for parsing the configuration file:
			this.elementBuilders = this.configuration.Container.Resolve<IReflection>()
				.FindConcreteTypesImplementingInterfaceAndBuild(typeof(BaseElementBuilder),
																GetType().Assembly);

			// parse configuration file and build worker pools:
			this.BuildInfrastructureVia<FabricWorkerPoolElementBuilder>();

			// need to fire the configuration of the endpoint once the message bus
			// endpoint is defined so we can match the endpoint options with the 
			// transport semantics:
			 ((Monaco.Configuration.Configuration)this.configuration).ConfigureEndpoint();

			// boot-up the minimum internal infrastructure:
			// this.RegisterInfrastructure();
		}

		private void FindBootableEndpointAndConfigure()
		{
			this.configuration = Monaco.Configuration.Configuration.Create();

			if (HasMultipleEndpointConfigurationsDefined())
			{
				// must inform client that only one endpoint can be used to boot the bus!!!
				throw new MonacoConfigurationException("There are multiple endpoint configurations defined to boot the service bus. " +
					"Please define only one endpoint configuration type to boot the service bus from the run-time directory.");
			}
			else
			{
				// use the endpoint configuration and local configuration to boot the bus:
				var endpointconfiguration = this.GetEndpointConfiguration();
				if (endpointconfiguration != null)
				{
					var endpointAssembly = endpointconfiguration.Item1;
					var endpointType = endpointconfiguration.Item2;

					var createdEndpointConfiguration = endpointAssembly
														.CreateInstance(endpointType.FullName) as ICanConfigureEndpoint;

					if (createdEndpointConfiguration != null)
					{
						createdEndpointConfiguration.Configure(this.configuration);
					}
				}
			}

			// configure the endpoint options and infrastructure based on selections:
			((Monaco.Configuration.Configuration)this.configuration).Configure();
		}

		private bool HasMultipleEndpointConfigurationsDefined()
		{
			int endpointConfigurationsCount = 0;

			// need to find endpoint configuration from run-time /bin folder:
			foreach (var file in this.files)
			{
				Assembly asm = this.LoadAssemblyFromFile(file);
				if (asm == null || asm == this.GetType().Assembly) continue;

				var types = FindTypesFromAssembly<ICanConfigureEndpoint>(asm);

				if (types != null && types.Item2.Count() > 1)
				{
					endpointConfigurationsCount++;
				}
			}

			// multiple endpoint configurations defined (will have to pick one to boot bus):
			return endpointConfigurationsCount > 1;
		}

		private Tuple<Assembly, Type> GetEndpointConfiguration()
		{
			Tuple<Assembly, Type> configuration = null;

			// need to find endpoint configuration from run-time /bin folder:
			foreach (var file in this.files)
			{
				Assembly asm = this.LoadAssemblyFromFile(file);
				if (asm == null || asm == this.GetType().Assembly) continue;

				var endpoint = (from type in asm.GetTypes()
								where typeof(ICanConfigureEndpoint).IsAssignableFrom(type)
								select type).FirstOrDefault();

				if (endpoint != null)
				{
					configuration = new Tuple<Assembly, Type>(asm, endpoint);
					break;
				}
			}

			return configuration;
		}

		private Assembly LoadAssemblyFromFile(string file)
		{
			Assembly target = null;
			//var logger = this.configuration.Container.Resolve<ILogger>();

			try
			{
				if (file.StartsWith("proxy")) return target;

				target = Assembly.LoadFile(file);

				if (target.FullName.Contains("proxy")) return target;
			}
			catch (FileNotFoundException fileNotFoundException)
			{
				//logger.LogErrorMessage("The following assembly " + file + " could not be found.", fileNotFoundException);
			}
			catch (FileLoadException fileLoadException)
			{
				//logger.LogErrorMessage("The following assembly " + file + " could not be loaded.", fileLoadException);
			}

			return target;
		}

		private Tuple<Assembly, IList<Type>> FindTypesFromAssembly<TType>(Assembly asm)
		{
			Tuple<Assembly, IList<Type>> typesWithAssembly = null;

			var types = (from type in asm.GetTypes()
						 where typeof(TType).IsAssignableFrom(type)
						 && type.IsClass == true
						 && type.IsAbstract == false
						 select type);

			typesWithAssembly = new Tuple<Assembly, IList<Type>>(asm, types.Distinct().ToList());

			return typesWithAssembly;
		}

		private void BuildInfrastructureVia<TElementBuilder>() where TElementBuilder : BaseElementBuilder
		{
			var builder = (from b in elementBuilders
						   where b.GetType() == typeof(TElementBuilder)
						   select b).FirstOrDefault() as BaseElementBuilder;

			if (builder == null) return;

			for (int index = 0; index < FacilityConfig.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration element = FacilityConfig.Children[index];

				if (element == null)
					continue;

				if (builder.IsMatchFor(element.Name))
				{
					builder.Container = this.configuration.Container;
					builder.Build(element);
					break;
				}
			}
		}

		/// <summary>
		/// This will wire-up the basic infrastructure needed by the distributor.
		/// </summary>
		private void RegisterInfrastructure()
		{
			configuration.Container.Register<IFabricWorkerPool, FabricWorkerPool>();
			configuration.Container.Register<IFabricWorkerPoolConfiguration, FabricWorkerPoolConfiguration>();
			configuration.Container.Register<IFabricWorkerPoolConfigurationRepository, FabricWorkerPoolConfigurationRepository>(ContainerLifeCycle.Singleton);
		}

		private Tuple<Assembly, IList<Type>> FindTypesFromAssemblyFile<TType>(string file)
		{
			Tuple<Assembly, IList<Type>> typesWithAssembly = null;

			Assembly asm = this.LoadAssemblyFromFile(file);
			if (asm == null) return typesWithAssembly;

			return FindTypesFromAssembly<TType>(asm);
		}


	}
}