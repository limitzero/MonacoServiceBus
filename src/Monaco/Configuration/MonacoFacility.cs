using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.MicroKernel.Facilities;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Reflection;
using Monaco.Configuration.Bootstrapper;
using Monaco.Configuration.Bootstrapper.Roles;
using Monaco.Configuration.Elements;
using Monaco.Configuration.Endpoint;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Extensibility.Logging;
using Monaco.Transport;

namespace Monaco.Configuration
{
	public class MonacoFacility : AbstractFacility
	{
		private ICanConfigureEndpoint bootedEndpoint;
		private IConfiguration configuration;
		public const string FACILITY_ID = "monaco.esb";
		private object[] elementBuilders = { };
		private BaseEndpointConfiguration endpointConfiguration;
		private string[] files = { };

		public MonacoFacility()
			:this(null)
		{
		}

		public MonacoFacility(ICanConfigureEndpoint bootedEndpoint)
		{
			this.bootedEndpoint = bootedEndpoint;
		}

		/// <summary>
		/// This will allow the facility to start and execute the single endpoint configuration 
		/// for initializing the bus instance and its local components.
		/// </summary>
		/// <param name="endpoint"></param>
		public void SetEndpoint(BaseEndpointConfiguration endpoint)
		{
			this.endpointConfiguration = endpoint;
		}

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

			if (this.elementBuilders.Length == 0)
			{
				throw new Exception("No elements for configuring the bus were defined in the attached configuration file. " +
									"Please refer to the sample configuration file for setting up the message bus and re-start the bus host instance.");
			}

			if (string.IsNullOrEmpty(this.configuration.EndpointName) == false)
			{
				BuildFromEndpointName();
			}
			else
			{
				// read the external settings after endpoint configuration has been defined
				this.BuildInfrastructureVia<MessageBusElementBuilder>();
				this.BuildInfrastructureVia<MessagesElementBuilder>();
				this.BuildInfrastructureVia<TasksElementBuilder>();
			}

			// need to fire the configuration of the endpoint once the message bus
			// endpoint is defined so we can match the endpoint options with the 
			// transport semantics:
			((Configuration)this.configuration).ConfigureEndpoint();

			// boot-up the internals for the bus:
			this.RunInternalBootstrappers();
		}

		private void BuildFromEndpointName()
		{
			Exchange exchange = this.configuration.Container.Resolve<IEndpointFactory>()
				.Build(this.configuration.EndpointName);

			if(exchange !=null)
			{
				// register the bus endpoint and use the endpoint setting on every instantiation:
				var errorEndpoint = new ServiceBusErrorEndpoint
				{
					Endpoint = new Uri(string.Concat(exchange.Transport.Endpoint.EndpointUri.OriginalString,".error"))
				};

				// register the error endpoint and use the endpoint setting:
				this.configuration.Container.RegisterInstance<IServiceBusErrorEndpoint>(errorEndpoint);

				// set up the transport for the bus:
				this.configuration.Container.RegisterInstance<ITransport>(exchange.Transport);
			}
		}

		private void FindBootableEndpointAndConfigure()
		{
			this.configuration = Configuration.Create();

			// check to see if an endpoint configuration was specfied on the container:
			if (this.bootedEndpoint != null)
			{
				this.bootedEndpoint.Configure(configuration);
			}
			else if (HasMultipleEndpointConfigurationsDefined())
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

					this.bootedEndpoint = endpointAssembly
					                                   	.CreateInstance(endpointType.FullName) as ICanConfigureEndpoint;

					if (bootedEndpoint != null)
					{
						bootedEndpoint.Configure(this.configuration);
					}
				}
			}

			// configure the endpoint options and infrastructure based on selections:
			((Configuration)this.configuration).Configure();
			((Configuration)this.configuration).ConfigureExtensibility();
		}

		private void RunInternalBootstrappers()
		{
			object[] items = this.configuration.Container.Resolve<IReflection>()
				.FindConcreteTypesImplementingInterfaceAndBuild(typeof(BaseInternalBootstrapper),
																GetType().Assembly);

			if (items.Length == 0) return;

			foreach (object item in items)
			{
				var bootstrapper = item as BaseBootstrapper;
				if (bootstrapper != null)
				{
					ExecuteBootstrapper(bootstrapper);
				}
			}
		}

		private void BuildInfrastructureVia<TElementBuilder>() where TElementBuilder : BaseElementBuilder
		{
			var builder = (from b in this.elementBuilders
						   where b.GetType() == typeof(TElementBuilder)
						   select b).FirstOrDefault() as BaseElementBuilder;

			for (int index = 0; index < FacilityConfig.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration element = FacilityConfig.Children[index];

				if (element == null)
					continue;

				if(builder != null && builder.IsMatchFor(element.Name))
				{
					builder.Container = this.configuration.Container;
					builder.Build(element);
					break;
				}
			}
		}

		private void ExecuteBootstrapper(BaseBootstrapper bootstrapper)
		{
			try
			{
				if (bootstrapper.IsActive == false) return;
				bootstrapper.Container = this.configuration.Container;
				bootstrapper.Configure();
			}
			catch (Exception e)
			{
				var logger = this.configuration.Container.Resolve<ILogger>();

				if (logger != null)
				{
					string msg = string.Format("The following boot-strapper '{0}' failed to execute. Reason: {1}",
											   bootstrapper.GetType().Name, e.Message);
					logger.LogWarnMessage(msg, e);
				}
				throw;
			}
		}

		private Assembly LoadAssemblyFromFile(string file)
		{
			Assembly target = null;
			//var logger = this.configuration.Container.Resolve<ILogger>();

			// resolve all assemblies that can not be done via current assembly via LINQ at runtime !!!
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				foreach (var appDomainAsembly in ((AppDomain)sender).GetAssemblies())
				{
					if (appDomainAsembly.FullName == args.Name)
					{
						return appDomainAsembly;
					}
				}
				return null;
			};

			try
			{
				if (file.StartsWith("proxy")) return target;

				target = Assembly.LoadFile(file);

				if (target.FullName.Contains("proxy")) return target;
			}
			catch
			{
			}
			//catch (FileNotFoundException fileNotFoundException)
			//{
			//    //logger.LogErrorMessage("The following assembly " + file + " could not be found.", fileNotFoundException);
			//}
			//catch (FileLoadException fileLoadException)
			//{
			//    //logger.LogErrorMessage("The following assembly " + file + " could not be loaded.", fileLoadException);
			//}

			return target;
		}

		private static List<Type> LoadAssemblyTypes(Assembly assembly)
		{
			var types = new List<Type>();

			try
			{
				types.AddRange(assembly.GetTypes());
			}
			catch
			{
			}

			return types;
		}

		private bool TryAddServiceToContainer(string key, Type service)
		{
			bool success = false;

			try
			{
				Kernel.AddComponent(key, service);
				success = true;
			}
			catch
			{
				// component is registered already:
			}

			return success;
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

				if(types != null && types.Item2.Count() > 1)
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

		private Tuple<Assembly, IList<Type>> FindTypesFromAssemblyFile<TType>(string file)
		{
			Tuple<Assembly, IList<Type>> typesWithAssembly = null;

			Assembly asm = this.LoadAssemblyFromFile(file);
			if (asm == null) return typesWithAssembly;

			return FindTypesFromAssembly<TType>(asm);
		}

		private static Tuple<Assembly, IList<Type>> FindTypesFromAssembly<TType>(Assembly asm)
		{
			Tuple<Assembly, IList<Type>> typesWithAssembly = null;

			try
			{
				var types = (from type in asm.GetTypes()
							 where typeof(TType).IsAssignableFrom(type)
							 && type.IsClass == true
							 && type.IsAbstract == false
							 select type);

				typesWithAssembly = new Tuple<Assembly, IList<Type>>(asm, types.Distinct().ToList());  
			}
			catch (ReflectionTypeLoadException ex)
			{
				StringBuilder sb = new StringBuilder();
				foreach (Exception exSub in ex.LoaderExceptions)
				{
					sb.AppendLine(exSub.Message);
					if (exSub is FileNotFoundException)
					{
						FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
						if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
						{
							sb.AppendLine("Fusion Log:");
							sb.AppendLine(exFileNotFound.FusionLog);
						}
					}
					sb.AppendLine();
				}
				string errorMessage = sb.ToString();
				System.Console.WriteLine(errorMessage);
			}
		
			return typesWithAssembly;
		}
	}
}