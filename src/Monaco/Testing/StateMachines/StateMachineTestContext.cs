using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Monaco.Bus;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Internals.Reflection.Impl;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines.Impl;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Configuration;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Logging.Impl;
using Monaco.Extensibility.Storage.Impl.Volatile;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.StateMachine;
using Monaco.StateMachine.Verbalizer.Impl;
using Monaco.Testing.Internals.Exceptions;
using Monaco.Testing.Internals.Interceptors.Impl;
using Monaco.Testing.StateMachines.Impl;
using Monaco.Testing.StateMachines.Internals;
using Monaco.Transport.Virtual;

namespace Monaco.Testing.StateMachines
{
	/// <summary>
	/// Basic class for testing interactions and processing for a state machine
	/// </summary>
	/// <typeparam name="TStateMachine">Type of state machine to test</typeparam>
	public class StateMachineTestContext<TStateMachine> : IDisposable
		where TStateMachine : SagaStateMachine
	{
		private Guid _correlationId;
		private IKernel _kernel;
		private TStateMachine _stateMachine;
		private IConfiguration configuration;

		public StateMachineTestContext()
		{
			// initialize the state machine:
			InitializeContext();
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (_kernel != null)
			{
				_kernel.Dispose();
			}
			_kernel = null;
		}

		#endregion

		/// <summary>
		/// This will register a state machine data finder for reconciling messages that have 
		/// custom correlations for retreiving state machine instance data.
		/// </summary>
		/// <typeparam name="TStateMachineDataFinder"></typeparam>
		protected void RegisterStateMachineDataFinder<TStateMachineDataFinder>()
			where TStateMachineDataFinder : class, IStateMachineDataFinder
		{
			foreach (Type @interface in typeof (TStateMachineDataFinder).GetInterfaces())
			{
				if (@interface.Name.StartsWith(typeof (IStateMachineDataFinder<,>).Name))
				{
					_kernel.AddComponent(string.Concat("{0}-{1}-{2}", @interface.Name,
					                                   @interface.GetGenericArguments()[1].Name,
					                                   @interface.GetGenericArguments()[0]),
					                     @interface,
					                     typeof (TStateMachineDataFinder));
				}
			}
		}

		/// <summary>
		/// This will run a verification of a set of expectations against the state machine for a given 
		/// message invocation as initiated by the "When..." action. 
		/// </summary>
		/// <param name="stateMachineTestScenarios"></param>
		/// <returns></returns>
		protected StateMachineTestContext<TStateMachine> Verify(params
		                                                        	IStateMachineTestScenario<TStateMachine>[]
		                                                        	stateMachineTestScenarios)
		{
			foreach (var stateMachineTestScenario in stateMachineTestScenarios)
			{
				stateMachineTestScenario.Verify();
			}
			return this;
		}

		/// <summary>
		/// This will extract the current localized data of the state machine for inspection (only avaliable afer call to <seealso cref="Verify"/>)
		/// </summary>
		/// <typeparam name="TStateMachineData">Type representing the state machine data</typeparam>
		/// <returns></returns>
		protected StateMachineTestContext<TStateMachine> GetStateMachineData<TStateMachineData>(
			out TStateMachineData stateMachineData)
			where TStateMachineData : class, IStateMachineData
		{
			stateMachineData = default(TStateMachineData);

			if (_stateMachine != null)
			{
				var property = _stateMachine.GetType().GetProperty("Data");

				if (property != null)
				{
					stateMachineData = property.GetValue(_stateMachine, null) as TStateMachineData;
				}
			}

			return this;
		}

		protected IStateMachineTestScenario<TStateMachine> When<TMessage>(
			Expression<Func<TStateMachine, Event<TMessage>>> @event)
			where TMessage : IMessage
		{
			return When<TMessage>(@event, null);
		}

		protected IStateMachineTestScenario<TStateMachine> When<TMessage>(
			Expression<Func<TStateMachine, Event<TMessage>>> @event,
			Action<TMessage> messageToConstructAction)
			where TMessage : IMessage
		{
			TMessage message = default(TMessage);

			IServiceBus mock = InitalizeServiceBusMockForExpectations();

			Action consumeAction = CreateConsumeAction(mock,
			                                           messageToConstructAction,
			                                           out message);

			var scenario = new StateMachineTestScenario<TStateMachine>(message,
			                                                           consumeAction,
			                                                           _stateMachine,
			                                                           _kernel,
			                                                           mock);

			return scenario;
		}

		/// <summary>
		/// This will cause all messages that are delayed for delivery to be retrieved and 
		/// routed to the state machine for processing in order, oldest to latest, one by one.
		/// </summary>
		/// <returns></returns>
		protected IStateMachineTestScenario<TStateMachine> WhenTimeoutIsFired()
		{
			IServiceBus mock = InitalizeServiceBusMockForExpectations();

			Func<IMessage> createAction = () =>
			                              	{
			                              		IMessage currentMessageToDeliver = null;

			                              		var repository = _kernel.Resolve<ITimeoutsRepository>();

			                              		// get all timeouts on the bus endpoint:
			                              		var timeouts = repository.FindAll(mock.Endpoint.EndpointUri.ToString());

			                              		// oldest first...
			                              		var timeout = timeouts.OrderBy(t => t.At).FirstOrDefault();

			                              		if (timeout != null)
			                              		{
			                              			currentMessageToDeliver = timeout.MessageToDeliver as IMessage;
			                              			repository.Remove(timeout);
			                              		}
			                              		else
			                              		{
			                              			throw new TimeoutInvocationException(
			                              				"No timeout was issued for the expectation to be verified.");
			                              		}

			                              		return currentMessageToDeliver;
			                              	};

			var scenario = new StateMachineTestScenario<TStateMachine>(null,
			                                                           createAction,
			                                                           null,
			                                                           _stateMachine,
			                                                           _kernel,
			                                                           mock);

			return scenario;
		}

		protected TMessage CreateMessage<TMessage>()
		{
			TMessage message = default(TMessage);
			message = _kernel.Resolve<IReflection>().CreateMessage<TMessage>();

			// TODO: remove commented code
			//if (typeof(TMessage).IsInterface)
			//{
			//    message = InitalizeServiceBusMockForExpectations().CreateMessage<TMessage>();
			//}
			//else
			//{
			//    message = (TMessage)typeof(TMessage).Assembly.CreateInstance(typeof(TMessage).FullName);
			//}

			return message;
		}

		private void InitializeContext()
		{
			_correlationId = Guid.NewGuid();

			this.configuration = Monaco.Configuration.Configuration.Create();

			this.configuration
				.WithContainer(c => c.UsingNullContainer())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingInMemory());
			

			// collaborators:
			_kernel = new DefaultKernel();
			_kernel.Register(Component.For<ILogger>().ImplementedBy<NullLogger>());

			_kernel.Register(Component.For<ISagaStateMachineMessageDispatcher>()
			                 	.ImplementedBy<SagaStateMachineMessageDispatcher>());

			_kernel.Register(Component.For<IReflection>().ImplementedBy<DefaultReflection>());

			_kernel.Register(Component.For<ISerializationProvider>()
			                 	.ImplementedBy<SharpSerializationProvider>());

			_kernel.AddComponent(typeof (IStateMachineDataRepository<>).Name,
			                     typeof (IStateMachineDataRepository<>),
			                     typeof (InMemoryStateMachineDataRepository<>),
			                     LifestyleType.Singleton);

			_kernel.Register(Component.For<ITimeoutsRepository>()
			                 	.ImplementedBy<InMemoryTimeoutsRepository>()
			                 	.LifeStyle.Singleton);

			//_kernel.Register(Component.For<IServiceBus>().Instance(_mock));

			_kernel.Register(Component.For<TStateMachine>());

			// subject under test:
			_stateMachine = _kernel.Resolve<TStateMachine>();
			_stateMachine.InstanceId = _correlationId;

			// intiialize other parts of the context for testing:
			SetSagaData(_stateMachine, _correlationId);
			VerbalizeStateMachine(_kernel);
		}

		private Action CreateConsumeAction<TMessage>(
			IServiceBus mockBus,
			Action<TMessage> messageConstructionAction,
			out TMessage messageToConsume)
			where TMessage : IMessage
		{
			var message = CreateMessage<TMessage>();

			if (messageConstructionAction != null)
			{
				messageConstructionAction(message);
			}

			IEnvelope envelope = new Envelope(message);

			Action consumeAction = () => _kernel.Resolve<ISagaStateMachineMessageDispatcher>()
			                             	.Dispatch(mockBus, _stateMachine, envelope);

			messageToConsume = message;

			return consumeAction;
		}

		private static void VerbalizeStateMachine(IKernel kernel)
		{
			try
			{
				var verbalizer = new SagaStateMachineVerbalizer();
				var statemachine = kernel.Resolve<TStateMachine>();
				Debug.WriteLine(verbalizer.Verbalize(statemachine));
			}
			catch
			{
			}
		}

		private void SetSagaData(SagaStateMachine stateMachine, Guid instanceId)
		{
			if (ContainsInterface(stateMachine, typeof (SagaStateMachine<>)) != null)
			{
				// need to instantiate new data for saga and sync the instance 
				// identifier with the instance identifier of the saga (testing of course):
				PropertyInfo theSagaDataProperty = GetSagaDataProperty(stateMachine);

				if (theSagaDataProperty == null) return;

				IReflection reflection = new DefaultReflection(_kernel);
				var theData = reflection.BuildInstance(theSagaDataProperty
				                                       	.PropertyType.AssemblyQualifiedName) as IStateMachineData;

				if (theData != null)
				{
					theData.Id = instanceId;
					theSagaDataProperty.SetValue(stateMachine, theData, null);
				}
			}
		}

		private void RegisterInterfaceBasedMessages()
		{
			var toRegister = new List<Type>();

			var interfaces = _stateMachine.GetType().GetInterfaces()
				.Where(i => i.Name.StartsWith(typeof (StartedBy<>).Name) ||
				            i.Name.StartsWith(typeof (OrchestratedBy<>).Name)).
				ToList().Distinct();

			foreach (var @interface in interfaces)
			{
				Type element = GetGenericElement(@interface);

				if (element != null && element.IsInterface)
					toRegister.Add(element);
			}

			foreach (Type @interface  in toRegister)
			{
				var interfaceStorage = new InterfacePersistance();
				var interceptor = new InterfaceInterceptor(interfaceStorage);

				var proxyGenerator = new ProxyGenerator();
				object proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget(@interface, interceptor);
				_kernel.AddComponentInstance(@interface.Name, @interface, proxy);
				//_kernel.AddComponent(@interface.Name, @interface, proxy.GetType(), LifestyleType.Transient);
			}

			//_kernel.Resolve<IReflection>().BuildProxyAssemblyForContracts(toRegister, true);
		}

		private static Type GetGenericElement(Type @interface)
		{
			Type element = null;

			if (@interface.IsGenericType)
			{
				element = @interface.GetGenericArguments()[0];
			}

			return element;
		}

		private static PropertyInfo GetSagaDataProperty(object theSaga)
		{
			PropertyInfo theDataProperty = null;

			if (ContainsInterface(theSaga, typeof (SagaStateMachine<>)) != null)
			{
				theDataProperty = (from property in theSaga.GetType().GetProperties()
				                   where typeof (IStateMachineData).IsAssignableFrom(property.PropertyType)
				                   select property).FirstOrDefault();
			}

			return theDataProperty;
		}

		private static Type ContainsInterface(object theComponent, Type interfaceType)
		{
			Type theInterface = null;

			if (interfaceType.IsClass)
			{
				if (theComponent.GetType().BaseType.Name.StartsWith(interfaceType.Name))
				{
					theInterface = theComponent.GetType().BaseType;
				}
			}
			else
			{
				theInterface = (from contract in theComponent.GetType().GetInterfaces()
				                where contract.IsGenericType
				                      &&
				                      contract.FullName.StartsWith(interfaceType.FullName)
				                select contract).FirstOrDefault();
			}

			return theInterface;
		}

		private IServiceBus InitalizeServiceBusMockForExpectations()
		{
			IServiceBus mock = MockFactory.CreateServiceBusMock(_kernel);

			// create the endpoint for the bus:
			var endpoint = new Uri(string.Format("vm://unit.test.{0}", _stateMachine.GetType().Name));
			var virtualEndpoint = new VirtualEndpoint {EndpointUri = endpoint};
			mock.SetEndpoint(virtualEndpoint);

			return mock;
		}
	}
}