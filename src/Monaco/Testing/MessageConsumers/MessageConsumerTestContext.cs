using System;
using System.Linq;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Monaco.Bus;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Internals.Reflection.Impl;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers.Impl;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Logging.Impl;
using Monaco.Extensibility.Storage.Impl.Volatile;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Testing.Internals.Exceptions;
using Monaco.Testing.MessageConsumers.Impl;
using Monaco.Testing.StateMachines.Internals;
using Monaco.Transport.Virtual;

namespace Monaco.Testing.MessageConsumers
{
	public class MessageConsumerTestContext<TMessageConsumer> : IDisposable
		where TMessageConsumer : MessageConsumer
	{
		private IKernel _kernel;
		private TMessageConsumer _messageConsumer;

		public MessageConsumerTestContext()
		{
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

		protected MessageConsumerTestContext<TMessageConsumer>
			Verify(params IMessageConsumerTestScenario<TMessageConsumer>[] scenarios)
		{
			foreach (var scenario in scenarios)
			{
				scenario.VerifyAll();
			}
			return this;
		}

		protected IMessageConsumerTestScenario<TMessageConsumer> When<TMessage>()
			where TMessage : IMessage
		{
			return When<TMessage>(null);
		}

		protected IMessageConsumerTestScenario<TMessageConsumer> When<TMessage>(
			Action<TMessage> messageConstructionAction) where TMessage : IMessage
		{
			TMessage message = default(TMessage);

			IServiceBus mock = InitalizeServiceBusMockForExpectations();

			Action consumeAction = CreateConsumeAction(mock,
			                                           messageConstructionAction,
			                                           out message);

			var scenario = new MessageConsumerTestScenario<TMessageConsumer>(message,
			                                                                 consumeAction,
			                                                                 _messageConsumer,
			                                                                 _kernel,
			                                                                 mock);

			return scenario;
		}


		/// <summary>
		/// This will cause all messages that are delayed for delivery to be retrieved and 
		/// routed to the state machine for processing in order, oldest to latest, one by one.
		/// </summary>
		/// <returns></returns>
		protected IMessageConsumerTestScenario<TMessageConsumer> WhenTimeoutIsFired()
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

			var scenario = new MessageConsumerTestScenario<TMessageConsumer>(null,
			                                                                 createAction,
			                                                                 null,
			                                                                 _messageConsumer,
			                                                                 _kernel,
			                                                                 mock);

			return scenario;
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

			Action consumeAction = () => _kernel.Resolve<ISimpleConsumerMessageDispatcher>()
			                             	.Dispatch(mockBus, _messageConsumer as IConsumer, envelope);

			messageToConsume = message;

			return consumeAction;
		}

		private void InitializeContext()
		{
			// collaborators (need this for component logging):
			_kernel = new DefaultKernel();

			_kernel.Register(Component.For<IReflection>()
								.ImplementedBy<DefaultReflection>());

			_kernel.Register(Component.For<ILogger>()
			                 	.ImplementedBy<NullLogger>());

			_kernel.Register(Component.For<ISerializationProvider>()
			                 	.ImplementedBy<SharpSerializationProvider>());

			_kernel.Register(Component.For<ISimpleConsumerMessageDispatcher>()
			                 	.ImplementedBy<SimpleConsumerMessageDispatcher>());

			_kernel.Register(Component.For<ITimeoutsRepository>()
			                 	.ImplementedBy<InMemoryTimeoutsRepository>()
			                 	.LifeStyle.Singleton);

			// subject under test:
			_kernel.Register(Component.For<TMessageConsumer>());

			_messageConsumer = _kernel.Resolve<TMessageConsumer>();
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

		private IServiceBus InitalizeServiceBusMockForExpectations()
		{
			IServiceBus mock = MockFactory.CreateServiceBusMock(_kernel);

			// create the endpoint for the bus:
			var endpoint = new Uri(string.Format("vm://unit.test.{0}", _messageConsumer.GetType().Name));
			var virtualEndpoint = new VirtualEndpoint {EndpointUri = endpoint};
			mock.SetEndpoint(virtualEndpoint);
			
			return mock;
		}
	}
}