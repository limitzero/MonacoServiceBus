using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Monaco.Bus.Agents.Scheduler;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration.Impl;
using Monaco.Bus.Internals;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Bus.Repositories;
using Monaco.Configuration.Registration;
using Monaco.Endpoint;
using Monaco.Endpoint.Factory;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensions;
using Monaco.StateMachine;
using Monaco.StateMachine.Persistance;
using Monaco.StateMachine.Verbalizer.Impl;
using Monaco.Subscriptions.Impl;
using Monaco.Transport;

namespace Monaco.Configuration.Impl
{
	internal class EndpointConfiguration : IEndpointConfiguration
	{
		private readonly IContainer container;
		private readonly IDictionary<Uri, Func<Type, bool>> messageDefinitions;

		public EndpointConfiguration(IContainer container)
		{
			this.container = container;
			this.messageDefinitions = new Dictionary<Uri, Func<Type, bool>>();
		}

		/// <summary>
		/// Gets or sets the filter expression for the messages that 
		/// are associated to a remote endpoint (by uri) which 
		/// will be involved in the publication/subscription model for the local service bus
		/// </summary>
		public IEndpointConfiguration ReceivingMessagesFrom(Uri endpoint, Func<Type, bool> messagesFilterExpression)
		{
			this.messageDefinitions.Add(endpoint, messagesFilterExpression);
			return this;
		}

		public IEndpointConfiguration ConfigureMessageFaultHandlerChain(Func<FaultHandlerConfiguration, FaultHandlerConfiguration> configuration)
		{
			var registry = this.container.Resolve<IFaultHandlerConfigurationRepository>();
			FaultHandlerConfiguration faultHandlerConfiguration = configuration(new FaultHandlerConfiguration());
			registry.Register(faultHandlerConfiguration);
			return this;
		}

		public IEndpointConfiguration ConfigureMessageConsumerHandlingChain(Func<HandlerConfiguration, HandlerConfiguration> configuration)
		{
			var registry = this.container.Resolve<IHandlerConfigurationRepository>();
			HandlerConfiguration handlerConfiguration = configuration(new HandlerConfiguration());
			registry.Register(handlerConfiguration);

			// register all configured consumers for the message:
			var registrar = this.container.Resolve<IRegisterConsumer>();

			foreach (var registration in registry.Registrations)
			{
				foreach (Type consumer in registration.Value.Consumers)
				{
					registrar.RegisterType(consumer);
				}
			}

			return this;
		}

		public IEndpointConfiguration SupportsTransactions(bool supportsTransactions)
		{
			var transport = this.container.Resolve<ITransport>();
			transport.IsTransactional = supportsTransactions;
			return this;
		}

		public IEndpointConfiguration SetTransactionIsolationLevel(IsolationLevel level)
		{
			var transport = this.container.Resolve<ITransport>();
			transport.TransactionIsolationLevel = level;
			return this;
		}

		public IEndpointConfiguration MapMessages<TConsumer>() where TConsumer : IConsumer
		{
			var transport = this.container.Resolve<ITransport>();
			return MapMessage<TConsumer>(transport.Endpoint.EndpointUri, true);
		}

		public IEndpointConfiguration MapMessages(string messageAssembly, Uri endpoint)
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
			return this;
		}

		public IEndpointConfiguration MapMessages(Uri endpoint, params IMessage[] messages)
		{
			List<Type> theMessages = (from message in messages
									  select message.GetType()).ToList();
			EnlistMessagesWithSubscriptionRepository(theMessages, endpoint.ToString());
			return this;
		}

		public IEndpointConfiguration MapMessage<TConsumer>(Uri endpoint, bool register) where TConsumer : IConsumer
		{
			MapMessage(typeof(TConsumer), endpoint, register);
			return this;
		}

		/// <summary>
		///  This will map a single  known message to a remote endpoint.
		/// </summary>
		/// <typeparam name="TMessage">Type of the messge to map to an endpoint</typeparam>
		/// <param name="endpoint">Uri corresponding a remote endpoint</param>
		public IEndpointConfiguration MapMessage<TMessage>(Uri endpoint) where TMessage : IMessage
		{
			var messages = new List<Type>();
			messages.Add(typeof(TMessage));
			EnlistMessagesWithSubscriptionRepository(messages, endpoint.ToString());
			return this;
		}

		public IEndpointConfiguration MapAll(Assembly assembly)
		{
			List<Type> types = (from type in assembly.GetTypes()
								where typeof(IConsumer).IsAssignableFrom(type)
									  && type.IsClass
									  && type.IsAbstract == false
								select type).Distinct().ToList();

			var transport = this.container.Resolve<ITransport>();

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

			return this;
		}

		public IEndpointConfiguration MapTask<TProducer>(string name, TimeSpan span) where TProducer : class, IProducer
		{
			return MapTask<TProducer>(name, span, true, true);
		}

		public IEndpointConfiguration MapTask<TProducer>(string name, TimeSpan span, bool haltOnError, bool forceStart) where TProducer : class, IProducer
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
				var producer = this.container.Resolve<TProducer>();
				throw new Exception(string.Format("Endpoint Configuration Error: " +
												  " The message producer '{0}' has already been defined on this endpoint instance",
												  typeof(TProducer).FullName));
			}
			catch
			{
			}

			string interval = span.ToInterval();

			ITaskConfiguration taskConfiguration = new TaskConfiguration();

			this.container.Register<TProducer>();

			taskConfiguration.TaskName = name;
			taskConfiguration.ComponentInstance = this.container.Resolve<TProducer>();
			taskConfiguration.MethodName = typeof(Produces<>).GetMethods()[0].Name;
			taskConfiguration.Interval = interval;
			taskConfiguration.HaltOnError = haltOnError;
			taskConfiguration.ForceStart = forceStart;

			var scheduler = this.container.Resolve<IScheduler>();

			scheduler.CreateFromConfiguration(taskConfiguration);

			return this;
		}

		public IEndpointConfiguration ConfigureStateMachineDataMergers(string assembly)
		{
			Assembly asm = Assembly.Load(assembly);
			return ConfigureStateMachineDataMergers(asm);
		}

		public IEndpointConfiguration ConfigureStateMachineDataMergers(params Assembly[] assemblies)
		{
			foreach (Assembly assembly in assemblies)
			{
				List<Type> stateMachineDataMergers = (from type in assembly.GetTypes()
													  where type.IsClass
															&& type.IsAbstract == false
															&& typeof(IStateMachineDataMerger).IsAssignableFrom(type)
													  select type).ToList();

				foreach (Type stateMachineDataMerger in stateMachineDataMergers)
				{
					foreach (Type @interface in stateMachineDataMerger.GetInterfaces())
					{
						if (@interface.Name.StartsWith(typeof(IStateMachineDataMerger<,>).Name))
						{
							this.container.Register(@interface, stateMachineDataMerger);
							//Kernel.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
							//                                  @interface.GetGenericArguments()[1].Name,
							//                                  @interface.GetGenericArguments()[0]),
							//                                  @interface,
							//                                  stateMachineDataFinder);
						}
					}
				}
			}

			return this;
		}

		public IEndpointConfiguration ConfigureStateMachineDataFinders(params Assembly[] assemblies)
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
							this.container.Register(@interface, stateMachineDataFinder);
							//Kernel.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
							//                                  @interface.GetGenericArguments()[1].Name,
							//                                  @interface.GetGenericArguments()[0]),
							//                                  @interface,
							//                                  stateMachineDataFinder);
						}
					}
				}
			}
			return this;
		}

		public IEndpointConfiguration ConfigureBusModules(Assembly assembly)
		{
			IEnumerable<Type> types = (from type in assembly.GetTypes()
									   where typeof(IBusModule).IsAssignableFrom(type)
									   select type).Distinct();

			foreach (Type type in types)
			{
				try
				{
					this.container.Register(type, ContainerLifeCycle.Singleton);
				}
				catch
				{
				}
			}
			return this;
		}

		public IEndpointConfiguration MapMessageModule<T>() where T : class, IMessageModule
		{
			try
			{
				this.container.Register<T>(ContainerLifeCycle.Singleton);
			}
			catch
			{
			}

			return this;
		}

		public void Configure()
		{
			this.MapAll(this.GetType().Assembly);
			this.MapRemoteMessageDefinitions();
			VerbalizeSagasOnEndpoint();
		}


		private void MapRemoteMessageDefinitions()
		{
			if (this.messageDefinitions.Count == 0) return;
			var assemblyFiles = Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "*.dll");

			var serviceBusReferencedAssemblies = (
			                                   	from file in assemblyFiles
			                                   	from assembly in Assembly.LoadFrom(file).GetReferencedAssemblies()
			                                   	where assembly.FullName.Equals(this.GetType().Assembly.FullName)
			                                   	select Assembly.LoadFrom(assembly.Name)).ToList().Distinct();

			var subscriptions = (from asm in serviceBusReferencedAssemblies
			                     from type in asm.GetExportedTypes()
			                     from definition in this.messageDefinitions
			                     where definition.Value(type) == true
			                     select
			                     	new Subscription
			                     		{
			                     			Id = CombGuid.NewGuid(),
			                     			Message = type.FullName,
			                     			Uri = definition.Key.OriginalString
			                     		}).ToList().Distinct();

			var subscriptionRepository = this.container.Resolve<ISubscriptionRepository>();

			foreach (var subscription in subscriptions)
			{
				subscriptionRepository.Register(subscription);		
			}
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
				this.container.Register(stateMachine);
				SagaStateMachine aStateMachine = null;

				try
				{
					aStateMachine = this.container.Resolve(stateMachine) as SagaStateMachine;
				}
				catch
				{
				}

				if (aStateMachine == null) continue;

				try
				{
					var results = verbalizer.Verbalize(aStateMachine);
					this.container.Resolve<ILogger>().LogInfoMessage(string.Concat(Environment.NewLine, results));
				}
				catch
				{
					continue;
				}
			}
		}

		private void MapMessage(Type consumer, Uri endpoint, bool register = false)
		{
			//IEnumerable<Type> theInterfaces = consumer.GetInterfaces()
			//       .Where(i => i.FullName.StartsWith(typeof(Consumes<>).FullName) ||
			//                   i.FullName.StartsWith(typeof(TransientConsumerOf<>).FullName) ||
			//                   i.FullName.StartsWith(typeof(StartedBy<>).FullName) ||
			//                   i.FullName.StartsWith(typeof(OrchestratedBy<>).FullName))
			//       .Select(i => i).ToList().Distinct();

			IEnumerable<Type> theInterfaces = consumer.GetInterfaces()
				   .Where(i => typeof(IConsumer).IsAssignableFrom(i))
				   .Select(i => i).ToList().Distinct();

			if (theInterfaces.Count() > 0)
			{
				IEnumerable<Type> consumingMessageInterfaces =
					theInterfaces.Where(i => i.IsGenericType == true)
						.Distinct().ToList();

				IEnumerable<Type> consumedMessages = (from anInterface in consumingMessageInterfaces
										  let aMessage = anInterface.GetGenericArguments()[0]
										  where anInterface.IsGenericType == true
										  select aMessage).Distinct().ToList();

				EnlistMessagesWithSubscriptionRepository(consumedMessages, endpoint.OriginalString, consumer);
			}

			if (register)
			{
				var consumerRegistrar = this.container.Resolve<IRegisterConsumer>();
				consumerRegistrar.RegisterType(consumer);
			}

			try
			{
				this.container.Resolve<ILogger>().LogDebugMessage(string.Format("Mapped message consumer '{0}' to endpoint '{1}'.",
																				consumer.FullName, endpoint));
			}
			catch
			{
			}
		}

		private void EnlistMessagesWithSubscriptionRepository(IEnumerable<Type> messages, string endpoint,
															  Type consumer = null)
		{
			var subscriptionRepository = this.container.Resolve<ISubscriptionRepository>();

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

		private static Exception CouldNotLoadMessageAssemblyException(string messageAssembly, Exception exception)
		{
			throw new Exception(
				string.Format("The assembly '{0}' containing the messages for one-time configuration could not be loaded.",
							  messageAssembly), exception);
		}
	}
}