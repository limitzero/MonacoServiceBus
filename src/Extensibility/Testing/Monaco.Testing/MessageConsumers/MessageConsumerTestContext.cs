using System;
using System.Linq;
using Monaco.Bus;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers;
using Monaco.Configuration;
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
		private TMessageConsumer _messageConsumer;
		private IConfiguration configuration;

		public MessageConsumerTestContext()
		{
			InitializeContext();
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (this.configuration != null)
			{
				if (this.configuration.Container != null)
				{
					this.configuration.Container.Dispose();
				}
			}
			this.configuration = null;
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
																			 this.configuration.Container,
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

												var repository = this.configuration.Container.Resolve<ITimeoutsRepository>();

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
																			 this.configuration.Container,
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

			Action consumeAction = () => this.configuration.Container.Resolve<ISimpleConsumerMessageDispatcher>()
											.Dispatch(mockBus, _messageConsumer as IConsumer, envelope);

			messageToConsume = message;

			return consumeAction;
		}

		private void InitializeContext()
		{
			this.configuration = Monaco.Configuration.Configuration.Create();

			this.configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingInMemory())
				.WithEndpoint(e => e.MapAll(typeof(TMessageConsumer).Assembly));

			// force the configuration for our options:
			((Configuration.Configuration)this.configuration).Configure();
			((Configuration.Configuration)this.configuration).ConfigureExtensibility();

			// subject under test:
			this.configuration.Container.Register<TMessageConsumer>();
			_messageConsumer = this.configuration.Container.Resolve<TMessageConsumer>();
		}

		protected TMessage CreateMessage<TMessage>()
		{
			TMessage message = default(TMessage);
			message = this.configuration.Container.Resolve<IReflection>().CreateMessage<TMessage>();
			return message;
		}

		private IServiceBus InitalizeServiceBusMockForExpectations()
		{
			IServiceBus mock = MockFactory.CreateServiceBusMock(this.configuration.Container);

			// create the endpoint for the bus:
			var endpoint = new Uri(string.Format("vm://unit.test.{0}", _messageConsumer.GetType().Name));
			var virtualEndpoint = new VirtualEndpoint { EndpointUri = endpoint };
			mock.SetEndpoint(virtualEndpoint);

			return mock;
		}
	}
}