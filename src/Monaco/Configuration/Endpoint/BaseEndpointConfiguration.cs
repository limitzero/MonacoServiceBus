using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Castle.Core;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration.Impl;
using Monaco.Bus.Internals;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Bus.Repositories;
using Monaco.Configuration.Bootstrapper.Roles;
using Monaco.Configuration.Profiles;
using Monaco.Configuration.Registration;
using Monaco.Endpoint;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.Impl.Volatile;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Extensions;
using Monaco.StateMachine;
using Monaco.StateMachine.Verbalizer.Impl;
using Monaco.Subscriptions.Impl;
using Monaco.Transport;

namespace Monaco.Configuration.Endpoint
{
	/// <summary>
	/// Base class for registering a series of message consumers as a logical endpoint.
	/// </summary>
	public abstract class BaseEndpointConfiguration
	{
		private bool isConfigured;
		private IConfiguration configuration;

		/// <summary>
		/// Gets or sets whether or not the endpoint configuration will be 
		/// accepted or active for the messaging endpoint for participation
		/// in the service bus for sending/receiving messages.
		/// </summary>
		public bool IsActive { get; set; }

		protected BaseEndpointConfiguration()
		{
			this.IsActive = true;
		}

		public abstract void ConfigureEndpoint(IConfiguration configuration);

		/// <summary>
		/// This will be called by internal infrastructure on bus start.
		/// </summary>
		public void Configure(IConfiguration configuration)
		{
			if (this.isConfigured == true) return;
			this.configuration = configuration; 

			ConfigureEndpoint(configuration);
			ConfigureBusModules();
			InspectProfile();
			VerbalizeSagasOnEndpoint();

			isConfigured = true;
		}

		/// <summary>
		/// This will configure a message to have one or more fault handlers in the event that it can not be 
		/// processed by the message consumer. The fault handlers will run in the sequence defined.
		/// </summary>
		/// <param name="configuration">Fault handler configuration for a message</param>
		protected void ConfigureFaultHandlers(Func<FaultHandlerConfiguration, FaultHandlerConfiguration> configuration)
		{
			var registry = this.configuration.Container.Resolve<IFaultHandlerConfigurationRepository>();
			FaultHandlerConfiguration faultHandlerConfiguration = configuration(new FaultHandlerConfiguration());
			registry.Register(faultHandlerConfiguration);
		}

		/// <summary>
		/// This will configure a message to be processed by a series of message consumers in a defined order.
		/// </summary>
		/// <param name="configuration">Handler configuration for a a message</param>
		public void ConfigureConsumers(Func<HandlerConfiguration, HandlerConfiguration> configuration)
		{
			var registry = this.configuration.Container.Resolve<IHandlerConfigurationRepository>();
			HandlerConfiguration handlerConfiguration = configuration(new HandlerConfiguration());
			registry.Register(handlerConfiguration);

			// register all configured consumers for the message:
			var registrar = this.configuration.Container.Resolve<IRegisterConsumer>();

			foreach (var registration in registry.Registrations)
			{
				foreach (Type consumer in registration.Value.Consumers)
				{
					registrar.RegisterType(consumer);
				}
			}
		}

		/// <summary>
		/// This will register a component in the underlying container.
		/// </summary>
		/// <typeparam name="TComponent">Type of the concrete class</typeparam>
		public void Register<TComponent>() where TComponent : class
		{
			try
			{
				this.configuration.Container.Register(typeof(TComponent), typeof(TComponent));
			}
			catch
			{
				// already registered, bypass:
				this.configuration.Container.Resolve<ILogger>().LogWarnMessage(
					string.Format("The component '{0}' is already registered in the container. Skipping...",
								  typeof(TComponent).FullName));
			}
		}

		/// <summary>
		/// This will register a component in the underlying container.
		/// </summary>
		/// <typeparam name="TContract">Type of the interface for the component</typeparam>
		/// <typeparam name="TService">Type of the concrete service implementing the contract</typeparam>
		public void Register<TContract, TService>() where TService : class, TContract
		{
			try
			{
				this.configuration.Container.Register(typeof(TContract), typeof(TService));
			}
			catch
			{
				// already registered, bypass:
				this.configuration.Container.Resolve<ILogger>().LogWarnMessage(
					string.Format("The component '{0}' is already registered in the container. Skipping...",
								  typeof(TContract).FullName));
			}
		}

		/// <summary>
		/// This will attempt to enforce transactional behavior 
		/// on the messaging transport (if supported). For items 
		/// that can participate with MSDTC, the transaction scope
		/// will be used, for others, they will receive an event when 
		/// the message is processed (without error) and can determine
		/// the disposal rules for the message at that point.
		/// </summary>
		/// <param name="supportsTransactions"></param>
		public void SupportsTransactions(bool supportsTransactions)
		{
			var transport = this.configuration.Container.Resolve<ITransport>();
			transport.IsTransactional = supportsTransactions;
		}

		/// <summary>
		/// This will allow the service bus to set the isolation level of the transaction
		/// when accessing transactional resources (only applied if the transport sets
		/// the "IsTransactional" flag to "true").
		/// </summary>
		/// <param name="level"></param>
		public void SetTransactionIsolationLevel(IsolationLevel level)
		{
			var transport = this.configuration.Container.Resolve<ITransport>();
			transport.TransactionIsolationLevel = level;
		}

		/// <summary>
		/// This will map a series of messages for a consumer to the local bus endpoint and 
		/// register the consumer in the container.
		/// </summary>
		/// <typeparam name="TConsumer">Consumer that will be connected to the bus endpoint for message receipt.</typeparam>
		public void MapMessages<TConsumer>() where TConsumer : IConsumer
		{
			var transport = this.configuration.Container.Resolve<ITransport>();
			MapMessages<TConsumer>(transport.Endpoint.EndpointUri, true);
		}

		/// <summary>
		///  This will map a series of messages to a remote endpoint from an assembly.
		/// </summary>
		/// <param name="messageAssembly">Assembly name of the messages that will be mapped to a remote endpoint</param>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		public void MapMessages(string messageAssembly, Uri endpoint)
		{
			Assembly asm = null;

			try
			{
				asm = Assembly.Load(messageAssembly);
			}
			catch (Exception exception)
			{
				throw CouldNotLoadMessageAssemblyException(messageAssembly, exception);
			}

			List<Type> messages = (from type in asm.GetTypes()
								   where typeof(IMessage).IsAssignableFrom(type) && !type.IsAbstract
								   select type).Distinct().ToList();

			EnlistMessagesWithSubscriptionRepository(messages, endpoint.ToString());
		}

		/// <summary>
		///  This will map a series of known messages to a remote endpoint.
		/// </summary>
		/// <param name="messages">Collection of messages to map to a remote endpoint.</param>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		public void MapMessages(Uri endpoint, params IMessage[] messages)
		{
			List<Type> theMessages = (from message in messages
									  select message.GetType()).ToList();

			EnlistMessagesWithSubscriptionRepository(theMessages, endpoint.ToString());
		}

		/// <summary>
		/// This will map a series of messages for a consumer to a remote endpoint.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer for the messages.</typeparam>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		/// <param name="register">Flag to indicate whether or not to automatically register the consumer in the container.</param>
		public void MapMessages<TConsumer>(Uri endpoint, bool register = false) where TConsumer : IConsumer
		{
			MapMessage(typeof(TConsumer), endpoint, register);
		}

		/// <summary>
		///  This will map a single  known message to a remote endpoint.
		/// </summary>
		/// <typeparam name="TMessage">Type of the messge to map to an endpoint</typeparam>
		/// <param name="endpoint">Uri corresponding a remote endpoint</param>
		public void MapMessage<TMessage>(Uri endpoint) where TMessage : IMessage
		{
			var messages = new List<Type>();
			messages.Add(typeof(TMessage));
			EnlistMessagesWithSubscriptionRepository(messages, endpoint.ToString());
		}

		/// <summary>
		/// This will map all message consumer messages to the local endpoint and register the 
		/// consumers in the underlying container.
		/// </summary>
		public void MapAll(Assembly assembly)
		{
			List<Type> types =
				(from type in assembly.GetTypes()
				 where typeof(IConsumer).IsAssignableFrom(type)
					   && type.IsClass
				 select type).Distinct().ToList();

			var transport = this.configuration.Container.Resolve<ITransport>();

			if (transport != null)
			{
				if (transport.Endpoint != null)
				{
					IEndpoint endpoint = transport.Endpoint;
					foreach (Type type in types)
					{
						MapMessage(type, endpoint.EndpointUri, true);
					}
				}
			}
		}

		/// <summary>
		/// This will register a message producing component as a 
		/// time-based task that will send a message out to the 
		/// service bus for others to consume.
		/// </summary>
		/// <typeparam name="TProducer">Component that produces a message on a given interval</typeparam>
		/// <param name="name">The name of the task</param>
		/// <param name="span">The interval that the task should run expressed as hours:minutes:seconds</param>
		public void MapTask<TProducer>(string name, TimeSpan span)
			where TProducer : class, IProducer
		{
			MapTask<TProducer>(name, span, true, true);
		}

		/// <summary>
		/// This will register a message producing component as a 
		/// time-based task that will send a message out to the 
		/// service bus for others to consume.
		/// </summary>
		/// <typeparam name="TProducer">Component that produces a message on a given interval</typeparam>
		/// <param name="name">The name of the task</param>
		/// <param name="span">The interval that the task should run expressed as hours:minutes:seconds</param>
		/// <param name="haltOnError">Flag to determine if the task should be halted if it generates an exception</param>
		/// <param name="forceStart">Flag to determine if the task should be called immediately when the bus is started.</param>
		public void MapTask<TProducer>(string name, TimeSpan span,
									   bool haltOnError = true,
									   bool forceStart = true) where TProducer : class, IProducer
		{
			if (typeof(TProducer).GetInterfaces().Length == 0)
				throw new Exception(string.Format("Endpont Configuraton Error: For defining a message producing task, " +
												  "the component '{0}' must inherit from '{1}'.  ",
												  typeof(TProducer).Name,
												  typeof(Produces<>).FullName));

			if (typeof(TProducer).GetInterfaces()[0].FullName.StartsWith(typeof(Produces<>).FullName) == false)
				throw new Exception(string.Format("Endpont Configuraton Error: For defining a message producing task, " +
												  "the component '{0}' must inherit from '{1}'.  ",
												  typeof(TProducer).Name,
												  typeof(Produces<>).FullName));

			try
			{
				var producer = this.configuration.Container.Resolve<TProducer>();
				throw new Exception(string.Format("Endpoint Configuration Error: " +
												  " The message producer '{0}' has already been defined on this endpoint instance",
												  typeof(TProducer).FullName));
			}
			catch
			{
			}

			string interval = span.ToInterval();

			ITaskConfiguration taskConfiguration = new TaskConfiguration();

			this.configuration.Container.Register<TProducer>();

			taskConfiguration.TaskName = name;
			taskConfiguration.ComponentInstance = this.configuration.Container.Resolve<TProducer>();
			taskConfiguration.MethodName = typeof(Produces<>).GetMethods()[0].Name;
			taskConfiguration.Interval = interval;
			taskConfiguration.HaltOnError = haltOnError;
			taskConfiguration.ForceStart = forceStart;

			var scheduler = this.configuration.Container.Resolve<IScheduler>();

			scheduler.CreateFromConfiguration(taskConfiguration);
		}

		/// <summary>
		/// This will register all custom state machine data finders for a given assembly by name.
		/// </summary>
		/// <param name="assembly">Name of the assembly</param>
		public void ConfigureStateMachineDataMergers(string assembly)
		{
			Assembly asm = Assembly.Load(assembly);
			ConfigureStateMachineDataMergers(asm);
		}

		/// <summary>
		/// This will register all custom state machine data mergers for a given set of assemblies.
		/// </summary>
		/// <param name="assemblies">Assemblies to search</param>
		public void ConfigureStateMachineDataMergers(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				List<Type> stateMachineDataMergers = (from type in assembly.GetTypes()
													  where type.IsClass
															&& type.IsAbstract == false
															&& typeof(IStateMachineDataMerger).IsAssignableFrom(type)
													  select type).ToList();

				foreach (Type stateMachineDataFinder in stateMachineDataMergers)
				{
					foreach (Type @interface in stateMachineDataFinder.GetInterfaces())
					{
						if (@interface.Name.StartsWith(typeof(IStateMachineDataMerger<,>).Name))
						{
							//Kernel.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
							//                                  @interface.GetGenericArguments()[1].Name,
							//                                  @interface.GetGenericArguments()[0]),
							//                                  @interface,
							//                                  stateMachineDataFinder);
						}
					}
				}
			}
		}

		/// <summary>
		/// This will register all custom state machine data finders for a given set of assemblies.
		/// </summary>
		/// <param name="assemblies">Assemblies to search</param>
		public void ConfigureStateMachineDataFinders(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				List<Type> stateMachineDataFinders = (from type in assembly.GetTypes()
													  where type.IsClass
															&& type.IsAbstract == false
															&& typeof(IStateMachineDataFinder).IsAssignableFrom(type)
													  select type).ToList();

				foreach (Type stateMachineDataFinder in stateMachineDataFinders)
				{
					foreach (Type @interface in stateMachineDataFinder.GetInterfaces())
					{
						if (@interface.Name.StartsWith(typeof(IStateMachineDataFinder<,>).Name))
						{
							//Kernel.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
							//                                  @interface.GetGenericArguments()[1].Name,
							//                                  @interface.GetGenericArguments()[0]),
							//                                  @interface,
							//                                  stateMachineDataFinder);
						}
					}
				}
			}
		}

		/// <summary>
		/// This will configure all of the local bus modules 
		/// for the endpoint.
		/// </summary>
		public void ConfigureBusModules()
		{
			IEnumerable<Type> types = (from type in GetType().Assembly.GetTypes()
									   where typeof(IBusModule).IsAssignableFrom(type)
									   select type).Distinct();

			foreach (Type type in types)
			{
				this.configuration.Container.Register(type,  ContainerLifeCycle.Singleton);
			}
		}

		/// <summary>
		/// This will configure the endpoint to use an in-memory 
		/// representation for the state machine data instance repositories instead of a 
		/// custom provider.
		/// </summary>
		public void UseDefaultStateMachineDataPersistance()
		{
			// this.configuration.Storage(s => s.UsingInMemoryStorage());
			//Kernel.AddComponent(typeof(IStateMachineDataRepository<>).Name,
			//                    typeof(IStateMachineDataRepository<>),
			//                    typeof(InMemoryStateMachineDataRepository<>),
			//                    LifestyleType.Transient);
		}

		/// <summary>
		/// This will add an on-demand endpoint transport to the service bus
		/// for sending or receiving messages on a different medium than the 
		/// service bus.
		/// </summary>
		public void UseEndpointTransport<TEndpointTransportRegistration>()
			where TEndpointTransportRegistration : class, IEndpointTransportRegistration, new()
		{
			// Deprecated: using the configuration
			//var factory = Kernel.Resolve<IEndpointFactory>();

			//if (factory == null) return;

			//factory.Register(new TEndpointTransportRegistration());
		}

		private void MapMessage(Type consumer, Uri endpoint, bool register = false)
		{
			List<Type> theInterfaces = (from type in consumer.GetInterfaces()
										where type.FullName.StartsWith(typeof(Consumes<>).FullName) ||
											  type.FullName.StartsWith(typeof(StartedBy<>).FullName) ||
											  type.FullName.StartsWith(typeof(OrchestratedBy<>).FullName) ||
											  type.FullName.StartsWith(typeof(TransientConsumerOf<>).FullName)
										select type).Distinct().ToList();

			if (theInterfaces.Count > 0)
			{
				List<Type> theMessages = (from anInterface in theInterfaces
										  let aMessage = anInterface.GetGenericArguments()[0]
										  select aMessage).Distinct().ToList();

				EnlistMessagesWithSubscriptionRepository(theMessages, endpoint.ToString(), consumer);
			}

			if (register)
			{
				var consumerRegistrar = this.configuration.Container.Resolve<IRegisterConsumer>();
				consumerRegistrar.RegisterType(consumer);
			}

			try
			{
				this.configuration.Container.Resolve<ILogger>().LogDebugMessage(string.Format("Mapped message consumer '{0}' to endpoint '{1}'.",
																		consumer.FullName, endpoint));
			}
			catch
			{
			}
		}

		private void EnlistMessagesWithSubscriptionRepository(IEnumerable<Type> messages, string endpoint,
															  Type consumer = null)
		{
			var subscriptionRepository = this.configuration.Container.Resolve<ISubscriptionRepository>();

			foreach (Type message in messages)
			{
				subscriptionRepository.Register(new Subscription
				{
					Component = consumer != null ? consumer.FullName : string.Empty,
					Message = message.FullName,
					IsActive = true,
					Uri = endpoint
				});
			}
		}

		private void InspectProfile()
		{
			//if (typeof(ILiteProfile).IsAssignableFrom(GetType()))
			//{
			//    // make the transport non-recoverable:
			//    this.configuration.Container.Resolve<ITransport>().IsRecoverable = false;
			//    UseDefaultStateMachineDataPersistance();
			//    UseDefaultDataPersistance();
			//}
			//if (typeof(IClientProfile).IsAssignableFrom(GetType()))
			//{
			//    // make the transport non-recoverable:
			//    Kernel.Resolve<ITransport>().IsRecoverable = false;
			//}

			//if (typeof(IServerProfile).IsAssignableFrom(GetType()))
			//{
			//    // make the transport recoverable:
			//    Kernel.Resolve<ITransport>().IsRecoverable = true;
			//}
		}

		private void UseDefaultDataPersistance()
		{
			//try
			//{
			//    Kernel.Resolve<ITimeoutsRepository>();
			//}
			//catch
			//{
			//    // use the in-memory implementation:
			//    Kernel.AddComponent(typeof(ITimeoutsRepository).Name, typeof(ITimeoutsRepository),
			//                        typeof(InMemoryTimeoutsRepository), LifestyleType.Singleton);
			//}

			//try
			//{
			//    Kernel.Resolve<ISubscriptionRepository>();
			//}
			//catch
			//{
			//    // use the in-memory implementation:
			//    Kernel.AddComponent(typeof(ISubscriptionRepository).Name, typeof(ISubscriptionRepository),
			//                        typeof(InMemorySubscriptionRepository), LifestyleType.Singleton);
			//}
		}

		private void VerbalizeSagasOnEndpoint()
		{
			List<Type> stateMachineTypes = (from stateMachine in GetType().Assembly.GetTypes()
											where typeof(SagaStateMachine).IsAssignableFrom(stateMachine)
												  && stateMachine.IsClass
												  && stateMachine.IsAbstract == false
											select stateMachine).Distinct().ToList();

			var verbalizer = new SagaStateMachineVerbalizer();

			foreach (Type stateMachine in stateMachineTypes)
			{
				this.configuration.Container.Register(stateMachine);

				//Kernel.AddComponent(stateMachine.Name, stateMachine);

				SagaStateMachine aStateMachine = null;

				try
				{
					aStateMachine = this.configuration.Container.Resolve(stateMachine) as SagaStateMachine;
				}
				catch
				{
				}

				if (aStateMachine == null) continue;

				try
				{
					var results = verbalizer.Verbalize(aStateMachine);
					this.configuration.Container.Resolve<ILogger>().LogInfoMessage(string.Concat(Environment.NewLine, results));
				}
				catch
				{
					continue;
				}
			}
		}

		private static Exception CouldNotLoadMessageAssemblyException(string messageAssembly, Exception exception)
		{
			throw new Exception(
				string.Format("The assembly '{0}' containing the messages for one-time configuration could not be loaded.",
							  messageAssembly), exception);
		}
	}

	/*
	public abstract class BaseEndpointConfiguration2 : BaseExternalBootstrapper
	{
		private bool isConfigured;

		public abstract void ConfigureEndpoint();

		/// <summary>
		/// This will be called by internal infrastructure on bus start.
		/// </summary>
		public override void Configure()
		{
			if(this.isConfigured == true) return;

			ConfigureEndpoint();
			ConfigureBusModules();
			InspectProfile();
			VerbalizeSagasOnEndpoint();

			isConfigured = true;
		}

		/// <summary>
		/// This will configure a message to have one or more fault handlers in the event that it can not be 
		/// processed by the message consumer. The fault handlers will run in the sequence defined.
		/// </summary>
		/// <param name="configuration">Fault handler configuration for a message</param>
		protected void ConfigureFaultHandlers(Func<FaultHandlerConfiguration, FaultHandlerConfiguration> configuration)
		{
			var registry = Container.Resolve<IFaultHandlerConfigurationRepository>();
			FaultHandlerConfiguration faultHandlerConfiguration = configuration(new FaultHandlerConfiguration());
			registry.Register(faultHandlerConfiguration);
		}

		/// <summary>
		/// This will configure a message to be processed by a series of message consumers in a defined order.
		/// </summary>
		/// <param name="configuration">Handler configuration for a a message</param>
		public void ConfigureConsumers(Func<HandlerConfiguration, HandlerConfiguration> configuration)
		{
			var registry = Container.Resolve<IHandlerConfigurationRepository>();
			HandlerConfiguration handlerConfiguration = configuration(new HandlerConfiguration());
			registry.Register(handlerConfiguration);

			// register all configured consumers for the message:
			var registrar = Container.Resolve<IRegisterConsumer>();

			foreach (var registration in registry.Registrations)
			{
				foreach (Type consumer in registration.Value.Consumers)
				{
					registrar.RegisterType(consumer);
				}
			}
		}

		/// <summary>
		/// This will register a component in the underlying container.
		/// </summary>
		/// <typeparam name="TComponent">Type of the concrete class</typeparam>
		public void Register<TComponent>() where TComponent : class
		{
			try
			{
				Container.AddComponent(typeof (TComponent).Name, typeof (TComponent));
			}
			catch
			{
				// already registered, bypass:
				Container.Resolve<ILogger>().LogWarnMessage(
					string.Format("The component '{0}' is already registered in the container. Skipping...",
					              typeof (TComponent).FullName));
			}
		}

		/// <summary>
		/// This will register a component in the underlying container.
		/// </summary>
		/// <typeparam name="TContract">Type of the interface for the component</typeparam>
		/// <typeparam name="TService">Type of the concrete service implementing the contract</typeparam>
		public void Register<TContract, TService>() where TService : class, TContract
		{
			try
			{
				Container.AddComponent(typeof (TContract).Name, typeof (TContract), typeof (TService));
			}
			catch
			{
				// already registered, bypass:
				Container.Resolve<ILogger>().LogWarnMessage(
					string.Format("The component '{0}' is already registered in the container. Skipping...",
					              typeof (TContract).FullName));
			}
		}

		/// <summary>
		/// This will attempt to enforce transactional behavior 
		/// on the messaging transport (if supported). For items 
		/// that can participate with MSDTC, the transaction scope
		/// will be used, for others, they will receive an event when 
		/// the message is processed (without error) and can determine
		/// the disposal rules for the message at that point.
		/// </summary>
		/// <param name="supportsTransactions"></param>
		public void SupportsTransactions(bool supportsTransactions)
		{
			var transport = Container.Resolve<ITransport>();
			transport.IsTransactional = supportsTransactions;
		}

		/// <summary>
		/// This will allow the service bus to set the isolation level of the transaction
		/// when accessing transactional resources (only applied if the transport sets
		/// the "IsTransactional" flag to "true").
		/// </summary>
		/// <param name="level"></param>
		public void SetTransactionIsolationLevel(IsolationLevel level)
		{
			var transport = Container.Resolve<ITransport>();
			transport.TransactionIsolationLevel = level;
		}

		/// <summary>
		/// This will map a series of messages for a consumer to the local bus endpoint and 
		/// register the consumer in the container.
		/// </summary>
		/// <typeparam name="TConsumer">Consumer that will be connected to the bus endpoint for message receipt.</typeparam>
		public void MapMessages<TConsumer>() where TConsumer : IConsumer
		{
			var transport = Container.Resolve<ITransport>();
			MapMessages<TConsumer>(transport.Endpoint.EndpointUri, true);
		}

		/// <summary>
		///  This will map a series of messages to a remote endpoint from an assembly.
		/// </summary>
		/// <param name="messageAssembly">Assembly name of the messages that will be mapped to a remote endpoint</param>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		public void MapMessages(string messageAssembly, Uri endpoint)
		{
			Assembly asm = null;

			try
			{
				asm = Assembly.Load(messageAssembly);
			}
			catch (Exception exception)
			{
				throw CouldNotLoadMessageAssemblyException(messageAssembly, exception);
			}

			List<Type> messages = (from type in asm.GetTypes()
			                       where typeof (IMessage).IsAssignableFrom(type) && !type.IsAbstract
			                       select type).Distinct().ToList();

			EnlistMessagesWithSubscriptionRepository(messages, endpoint.ToString());
		}

		/// <summary>
		///  This will map a series of known messages to a remote endpoint.
		/// </summary>
		/// <param name="messages">Collection of messages to map to a remote endpoint.</param>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		public void MapMessages(Uri endpoint, params IMessage[] messages)
		{
			List<Type> theMessages = (from message in messages
			                          select message.GetType()).ToList();

			EnlistMessagesWithSubscriptionRepository(theMessages, endpoint.ToString());
		}

		/// <summary>
		/// This will map a series of messages for a consumer to a remote endpoint.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer for the messages.</typeparam>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		/// <param name="register">Flag to indicate whether or not to automatically register the consumer in the container.</param>
		public void MapMessages<TConsumer>(Uri endpoint, bool register = false) where TConsumer : IConsumer
		{
			MapMessage(typeof (TConsumer), endpoint, register);
		}

		/// <summary>
		///  This will map a single  known message to a remote endpoint.
		/// </summary>
		/// <typeparam name="TMessage">Type of the messge to map to an endpoint</typeparam>
		/// <param name="endpoint">Uri corresponding a remote endpoint</param>
		public void MapMessage<TMessage>(Uri endpoint) where TMessage : IMessage
		{
			var messages = new List<Type>();
			messages.Add(typeof (TMessage));
			EnlistMessagesWithSubscriptionRepository(messages, endpoint.ToString());
		}

		/// <summary>
		/// This will map all message consumer messages to the local endpoint and register the 
		/// consumers in the underlying container.
		/// </summary>
		public void MapAll(Assembly assembly)
		{
			List<Type> types =
				(from type in assembly.GetTypes()
				 where typeof (IConsumer).IsAssignableFrom(type)
				       && type.IsClass
				 select type).Distinct().ToList();

			var transport = Container.Resolve<ITransport>();

			if (transport != null)
			{
				if (transport.Endpoint != null)
				{
					IEndpoint endpoint = transport.Endpoint;
					foreach (Type type in types)
					{
						MapMessage(type, endpoint.EndpointUri, true);
					}
				}
			}
		}

		/// <summary>
		/// This will register a message producing component as a 
		/// time-based task that will send a message out to the 
		/// service bus for others to consume.
		/// </summary>
		/// <typeparam name="TProducer">Component that produces a message on a given interval</typeparam>
		/// <param name="name">The name of the task</param>
		/// <param name="span">The interval that the task should run expressed as hours:minutes:seconds</param>
		public void MapTask<TProducer>(string name, TimeSpan span)
			where TProducer : IProducer
		{
			MapTask<TProducer>(name, span, true, true);
		}

		/// <summary>
		/// This will register a message producing component as a 
		/// time-based task that will send a message out to the 
		/// service bus for others to consume.
		/// </summary>
		/// <typeparam name="TProducer">Component that produces a message on a given interval</typeparam>
		/// <param name="name">The name of the task</param>
		/// <param name="span">The interval that the task should run expressed as hours:minutes:seconds</param>
		/// <param name="haltOnError">Flag to determine if the task should be halted if it generates an exception</param>
		/// <param name="forceStart">Flag to determine if the task should be called immediately when the bus is started.</param>
		public void MapTask<TProducer>(string name, TimeSpan span,
		                               bool haltOnError = true,
		                               bool forceStart = true) where TProducer : IProducer
		{
			if (typeof (TProducer).GetInterfaces().Length == 0)
				throw new Exception(string.Format("Endpont Configuraton Error: For defining a message producing task, " +
				                                  "the component '{0}' must inherit from '{1}'.  ",
				                                  typeof (TProducer).Name,
				                                  typeof (Produces<>).FullName));

			if (typeof (TProducer).GetInterfaces()[0].FullName.StartsWith(typeof (Produces<>).FullName) == false)
				throw new Exception(string.Format("Endpont Configuraton Error: For defining a message producing task, " +
				                                  "the component '{0}' must inherit from '{1}'.  ",
				                                  typeof (TProducer).Name,
				                                  typeof (Produces<>).FullName));

			try
			{
				var producer = Container.Resolve<TProducer>();
				throw new Exception(string.Format("Endpoint Configuration Error: " +
				                                  " The message producer '{0}' has already been defined on this endpoint instance",
				                                  typeof (TProducer).FullName));
			}
			catch
			{
			}

			string interval = span.ToInterval();

			ITaskConfiguration taskConfiguration = new TaskConfiguration();

			Container.AddComponent(typeof (TProducer).Name, typeof (TProducer));

			taskConfiguration.TaskName = name;
			taskConfiguration.ComponentInstance = Container.Resolve<TProducer>();
			taskConfiguration.MethodName = typeof (Produces<>).GetMethods()[0].Name;
			taskConfiguration.Interval = interval;
			taskConfiguration.HaltOnError = haltOnError;
			taskConfiguration.ForceStart = forceStart;

			var scheduler = Container.Resolve<IScheduler>();

			scheduler.CreateFromConfiguration(taskConfiguration);
		}

		/// <summary>
		/// This will register all custom state machine data finders for a given assembly by name.
		/// </summary>
		/// <param name="assembly">Name of the assembly</param>
		public void ConfigureStateMachineDataMergers(string assembly)
		{
			Assembly asm = Assembly.Load(assembly);
			ConfigureStateMachineDataMergers(asm);
		}

		/// <summary>
		/// This will register all custom state machine data mergers for a given set of assemblies.
		/// </summary>
		/// <param name="assemblies">Assemblies to search</param>
		public void ConfigureStateMachineDataMergers(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				List<Type> stateMachineDataMergers = (from type in assembly.GetTypes()
				                                      where type.IsClass
				                                            && type.IsAbstract == false
				                                            && typeof (IStateMachineDataMerger).IsAssignableFrom(type)
				                                      select type).ToList();

				foreach (Type stateMachineDataFinder in stateMachineDataMergers)
				{
					foreach (Type @interface in stateMachineDataFinder.GetInterfaces())
					{
						if (@interface.Name.StartsWith(typeof(IStateMachineDataMerger<,>).Name))
						{
							Container.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
							                                  @interface.GetGenericArguments()[1].Name,
							                                  @interface.GetGenericArguments()[0]),
							                                  @interface,
							                                  stateMachineDataFinder);
						}
					}
				}
			}
		}

		/// <summary>
		/// This will register all custom state machine data finders for a given set of assemblies.
		/// </summary>
		/// <param name="assemblies">Assemblies to search</param>
		public void ConfigureStateMachineDataFinders(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				List<Type> stateMachineDataFinders = (from type in assembly.GetTypes()
													  where type.IsClass
															&& type.IsAbstract == false
															&& typeof(IStateMachineDataFinder).IsAssignableFrom(type)
													  select type).ToList();

				foreach (Type stateMachineDataFinder in stateMachineDataFinders)
				{
					foreach (Type @interface in stateMachineDataFinder.GetInterfaces())
					{
						if (@interface.Name.StartsWith(typeof(IStateMachineDataFinder<,>).Name))
						{
							Container.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
															  @interface.GetGenericArguments()[1].Name,
															  @interface.GetGenericArguments()[0]),
															  @interface,
															  stateMachineDataFinder);
						}
					}
				}
			}
		}

		/// <summary>
		/// This will configure all of the local bus modules 
		/// for the endpoint.
		/// </summary>
		public void ConfigureBusModules()
		{
			IEnumerable<Type> types = (from type in GetType().Assembly.GetTypes()
			                           where typeof (IBusModule).IsAssignableFrom(type)
			                           select type).Distinct();

			foreach (Type type in types)
			{
				Container.AddComponent(type.Name, type, LifestyleType.Singleton);
			}
		}

		/// <summary>
		/// This will configure the endpoint to use an in-memory 
		/// representation for the state machine data instance repositories instead of a 
		/// custom provider.
		/// </summary>
		public void UseDefaultStateMachineDataPersistance()
		{
			Container.AddComponent(typeof (IStateMachineDataRepository<>).Name,
			                    typeof (IStateMachineDataRepository<>),
			                    typeof (InMemoryStateMachineDataRepository<>),
			                    LifestyleType.Transient);
		}

		/// <summary>
		/// This will add an on-demand endpoint transport to the service bus
		/// for sending or receiving messages on a different medium than the 
		/// service bus.
		/// </summary>
		public void UseEndpointTransport<TEndpointTransportRegistration>()
			where TEndpointTransportRegistration : class, IEndpointTransportRegistration, new()
		{
			var factory = Container.Resolve<IEndpointFactory>();

			if (factory == null) return;

			factory.Register(new TEndpointTransportRegistration());
		}

		private void MapMessage(Type consumer, Uri endpoint, bool register = false)
		{
			List<Type> theInterfaces = (from type in consumer.GetInterfaces()
			                            where type.FullName.StartsWith(typeof (Consumes<>).FullName) ||
			                                  type.FullName.StartsWith(typeof (StartedBy<>).FullName) ||
			                                  type.FullName.StartsWith(typeof (OrchestratedBy<>).FullName) ||
			                                  type.FullName.StartsWith(typeof (TransientConsumerOf<>).FullName)
			                            select type).Distinct().ToList();

			if (theInterfaces.Count > 0)
			{
				List<Type> theMessages = (from anInterface in theInterfaces
				                          let aMessage = anInterface.GetGenericArguments()[0]
				                          select aMessage).Distinct().ToList();

				EnlistMessagesWithSubscriptionRepository(theMessages, endpoint.ToString(), consumer);
			}

			if (register)
			{
				var consumerRegistrar = Container.Resolve<IRegisterConsumer>();
				consumerRegistrar.RegisterType(consumer);
			}

			try
			{
				Container.Resolve<ILogger>().LogDebugMessage(string.Format("Mapped message consumer '{0}' to endpoint '{1}'.",
				                                                        consumer.FullName, endpoint));
			}
			catch
			{
			}
		}

		private void EnlistMessagesWithSubscriptionRepository(IEnumerable<Type> messages, string endpoint,
		                                                      Type consumer = null)
		{
			var subscriptionRepository = Container.Resolve<ISubscriptionRepository>();

			foreach (Type message in messages)
			{
				subscriptionRepository.Register(new Subscription
				                                	{
				                                		Component = consumer != null ? consumer.FullName : string.Empty,
				                                		Message = message.FullName,
				                                		IsActive = true,
				                                		Uri = endpoint
				                                	});
			}
		}

		private void InspectProfile()
		{
			if (typeof (ILiteProfile).IsAssignableFrom(GetType()))
			{
				// make the transport non-recoverable:
				Container.Resolve<ITransport>().IsRecoverable = false;
				UseDefaultStateMachineDataPersistance();
				UseDefaultDataPersistance();
			}
			if (typeof (IClientProfile).IsAssignableFrom(GetType()))
			{
				// make the transport non-recoverable:
				Container.Resolve<ITransport>().IsRecoverable = false;
			}

			if (typeof (IServerProfile).IsAssignableFrom(GetType()))
			{
				// make the transport recoverable:
				Container.Resolve<ITransport>().IsRecoverable = true;
			}
		}

		private void UseDefaultDataPersistance()
		{
			try
			{
				Container.Resolve<ITimeoutsRepository>();
			}
			catch
			{
				// use the in-memory implementation:
				Container.AddComponent(typeof(ITimeoutsRepository).Name, typeof(ITimeoutsRepository),
									typeof(InMemoryTimeoutsRepository), LifestyleType.Singleton);
			}

			try
			{
				Container.Resolve<ISubscriptionRepository>();
			}
			catch
			{
				// use the in-memory implementation:
				Container.AddComponent(typeof(ISubscriptionRepository).Name, typeof(ISubscriptionRepository),
									typeof(InMemorySubscriptionRepository), LifestyleType.Singleton);
			}
		}

		private void VerbalizeSagasOnEndpoint()
		{
			List<Type> stateMachineTypes = (from stateMachine in GetType().Assembly.GetTypes()
			                                where typeof (SagaStateMachine).IsAssignableFrom(stateMachine)
			                                      && stateMachine.IsClass
			                                      && stateMachine.IsAbstract == false
			                                select stateMachine).Distinct().ToList();

			var verbalizer = new SagaStateMachineVerbalizer();

			foreach (Type stateMachine in stateMachineTypes)
			{
				Container.AddComponent(stateMachine.Name, stateMachine);

				SagaStateMachine aStateMachine = null;

				try
				{
					aStateMachine = Container.Resolve(stateMachine) as SagaStateMachine;
				}
				catch
				{
				}

				if (aStateMachine == null) continue;

				try
				{
					var results = verbalizer.Verbalize(aStateMachine);
					Container.Resolve<ILogger>().LogInfoMessage(string.Concat(Environment.NewLine, results));
				}
				catch
				{
					continue;
				}
			}
		}

		private static Exception CouldNotLoadMessageAssemblyException(string messageAssembly, Exception exception)
		{
			throw new Exception(
				string.Format("The assembly '{0}' containing the messages for one-time configuration could not be loaded.",
				              messageAssembly), exception);
		}
	}
	 */ 
}