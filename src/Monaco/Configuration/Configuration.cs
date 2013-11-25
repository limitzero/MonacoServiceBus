using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Monaco.Bus;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Internals.Reflection.Impl;
using Monaco.Bus.MessageManagement.Callbacks;
using Monaco.Bus.MessageManagement.Dispatcher;
using Monaco.Bus.MessageManagement.Dispatcher.Impl;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers.Impl;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines.Impl;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Bus.MessageManagement.Pipeline;
using Monaco.Bus.MessageManagement.Pipeline.Impl;
using Monaco.Bus.MessageManagement.Resolving;
using Monaco.Bus.MessageManagement.Resolving.Impl;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Bus.Persistance.Callbacks;
using Monaco.Bus.Persistance.FaultHandlers;
using Monaco.Bus.Persistance.Handlers;
using Monaco.Bus.Repositories;
using Monaco.Bus.Services.Timeout;
using Monaco.Configuration.Impl;
using Monaco.Configuration.Registration;
using Monaco.Configuration.Registration.Impl;
using Monaco.Endpoint.Factory;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Logging.Impl;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Transport.Virtual;

namespace Monaco.Configuration
{
	public class Configuration : IConfiguration
	{
		private Expression<Func<IEndpointConfiguration, IEndpointConfiguration>> endpointConfigurationExpression;
		public ITransportConfiguration TransportConfiguration { get; private set; }
		public IStorageConfiguration StorageConfiguration { get; private set; }
		public IEndpointConfiguration EndpointConfiguration { get; private set; }
		public IContainer Container { get; private set; }
		public string EndpointName { get; private set; }
	
		private readonly List<Action> extensibilityActions; 

		private Configuration()
		{
			this.extensibilityActions = new List<Action>();
		}

		/// <summary>
		/// Retrieves the current instance of the configuration and optionally sets 
		/// the configuration file to read the settings.
		/// </summary>
		/// <returns></returns>
		public static IConfiguration Instance
		{
			get { return Factory.Instance; }
		}

		/// <summary>
		/// This will only be used by the infrastructure to pass in a new 
		/// configuration instance to the endpoint for customization.
		/// </summary>
		/// <returns></returns>
		public static IConfiguration Create()
		{
			return new Configuration();
		}

		public IConfiguration With(IContainer container)
		{
			this.Container = container;
			return this;
		}

		public IConfiguration WithEndpointName(string name)
		{
			this.EndpointName = name;
			return this;
		}

		public IConfiguration WithContainer(Expression<Func<IContainerConfiguration, IContainerConfiguration>> configuration)
		{
			var containerConfiguration = configuration.Compile().Invoke(new ContainerConfiguration());

			if (containerConfiguration != null)
			{
				this.Container = containerConfiguration.Container;
			}

			return this;
		}

		public IConfiguration WithStorage(Expression<Func<IStorageConfiguration, IStorageConfiguration>> configuration)
		{
			this.StorageConfiguration = configuration.Compile().Invoke(new StorageConfiguration(this));
			return this;
		}

		public IConfiguration WithTransport(Expression<Func<ITransportConfiguration, ITransportConfiguration>> configuration)
		{
			this.TransportConfiguration = configuration.Compile().Invoke(new TransportConfiguration(this.Container));
			return this;
		}

		public IConfiguration WithEndpoint(Expression<Func<IEndpointConfiguration, IEndpointConfiguration>> configuration)
		{
			this.endpointConfigurationExpression = configuration;
			return this;
		}

		/// <summary>
		/// This will configure the service bus and other components based on the user-specified configuration 
		/// and the settings located in the corresponding configuration file or application configuration file.
		/// </summary>
		public void Configure()
		{
			this.RegisterInfrastructure();
			this.RegisterStorage();
			this.RegisterTransport();
		}

		/// <summary>
		/// This will configure the service bus transport and endpoint options. It must be called after the 
		/// service bus endpoint is defined from the configuration settings.
		/// </summary>
		public void ConfigureEndpoint()
		{
			this.RegisterEndpoint();
		}

		public void ConfigureAll()
		{
			this.Configure();
			this.ConfigureEndpoint();
		}

		public void ConfigureExtensibility()
		{
			this.extensibilityActions.ForEach(ex => ex.Invoke());
			this.extensibilityActions.Clear();
		}

		public void BindExtensibilityAction(Action extensibilityAction)
		{
			this.extensibilityActions.Add(extensibilityAction);
		}

		private void RegisterStorage()
		{
			GuardAgainst(() => this.StorageConfiguration.TimeoutsRepository == null,
						 "The type for storing the timeout-related messages was not defined for the endpoint.");

			GuardAgainst(() => this.StorageConfiguration.SubscriptionRepository == null,
						 "The type for storing the subscriptions was not defined for the endpoint.");

			GuardAgainst(() => this.StorageConfiguration.StateMachineDataRepository == null,
						 "The type for storing the state machine instance data was not defined for the endpoint.");

			this.Container.Register(typeof(IStateMachineDataRepository<>),
				this.StorageConfiguration.StateMachineDataRepository,
				this.StorageConfiguration.StateMachineDataRepositoryContainerLifeCycle);

			this.Container.Register(typeof(ITimeoutsRepository),
				this.StorageConfiguration.TimeoutsRepository, 
				this.StorageConfiguration.TimeoutsRepositoryContainerLifeCycle);

			this.Container.Register(typeof(ISubscriptionRepository), 
				this.StorageConfiguration.SubscriptionRepository, 
				this.StorageConfiguration.SubscriptionRepositoryContainerLifeCycle);
		}

		private void RegisterTransport()
		{
			GuardAgainst(() => this.TransportConfiguration.Registration == null,
						 "The type for storing the registration information for the endpoint and transport was not specified.");
			this.Container.Resolve<IEndpointFactory>().Register(this.TransportConfiguration.Registration);
			this.Container.Resolve<IEndpointFactory>().Register(new VirtualEndpointTransportRegistration());
		}

		private void RegisterEndpoint()
		{
			this.EndpointConfiguration = this.endpointConfigurationExpression
				.Compile().Invoke(new EndpointConfiguration(this.Container));

			GuardAgainst(() => this.EndpointConfiguration == null,
						 "There was not an endpoint configuration set to use the current library as a messaging endpoint.");
			((EndpointConfiguration)this.EndpointConfiguration).Configure();
		}

		/// <summary>
		/// This will wire-up the basic infrastructure needed by the service bus.
		/// </summary>
		private void RegisterInfrastructure()
		{
			this.Container.RegisterInstance<IContainer>(this.Container);
			this.Container.Register<ILogger, Log4NetLogger>();
			this.Container.Register<IReflection, DefaultReflection>();
			this.Container.Register<ISerializationProvider, SharpSerializationProvider>();
			this.Container.Register<IEndpointFactory, EndpointFactory>(ContainerLifeCycle.Singleton);
			this.Container.Register<IMessageDispatcher, MessageDispatcher>();
			this.Container.Register<ISagaStateMachineMessageDispatcher, SagaStateMachineMessageDispatcher>();
			this.Container.Register<ISimpleConsumerMessageDispatcher, SimpleConsumerMessageDispatcher>();
			this.Container.Register<IScheduler, Scheduler>(ContainerLifeCycle.Singleton);
			this.Container.Register<ITimeoutsService, TimeoutsService>(ContainerLifeCycle.Singleton);
			this.Container.Register<IHandlerConfigurationRepository, LocalHandlerConfigurationRepository>(ContainerLifeCycle.Singleton);
			this.Container.Register<IFaultHandlerConfigurationRepository, FaultHandlerConfigurationRepository>(ContainerLifeCycle.Singleton);
			this.Container.Register<IFaultProcessor, FaultProcessor>();
			this.Container.Register<ICallback, ServiceBusCallback>();
			this.Container.Register<ICallBackRepository, LocalCallBackRepository>();
			this.Container.Register<IRegisterConsumer, RegisterConsumer>();
			this.Container.Register<IResolveMessageToConsumers, ResolveMessageToConsumers>();
			this.Container.Register<IPipeline, DefaultPipeline>();
			this.Container.Register<IOneWayBus, OneWayBus>();
			this.Container.Register<IControlBus, ControlBus>();
			this.Container.Register<IServiceBus, DefaultServiceBus>();
		}

		private static bool GuardAgainst(Func<bool> guard, string message = "")
		{
			bool result = guard();

			if (result == true && string.IsNullOrEmpty(message) == false)
			{
				throw new InvalidOperationException(message);
			}

			return result;
		}

		private class Factory
		{
			internal static readonly Configuration Instance = new Configuration();
		}
	}
}