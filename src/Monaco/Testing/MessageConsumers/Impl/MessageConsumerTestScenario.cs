using System;
using System.Collections.Generic;
using Castle.MicroKernel;
using Monaco.Extensions;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.MessageConsumers.Impl
{
	public class MessageConsumerTestScenario<TMessageConsumer> :
		IMessageConsumerTestScenario<TMessageConsumer>
		where TMessageConsumer : MessageConsumer
	{
		private readonly List<Action<IMessage>> _verifiables;

		public MessageConsumerTestScenario(
			IMessage consumedMessage,
			Action consumeMessageAction,
			TMessageConsumer messageConsumer,
			IKernel kernel,
			IServiceBus mockServiceBus)
			: this(consumedMessage, null, consumeMessageAction, messageConsumer, kernel, mockServiceBus)
		{
		}

		public MessageConsumerTestScenario(
			IMessage consumedMessage,
			Func<IMessage> externalConstructMessageAction,
			Action consumeMessageAction,
			TMessageConsumer messageConsumer,
			IKernel kernel,
			IServiceBus mockServiceBus)
		{
			ConsumedMessage = consumedMessage;
			ExternalConstructMessageAction = externalConstructMessageAction;
			ConsumeMessageAction = consumeMessageAction;
			MessageConsumer = messageConsumer;
			Kernel = kernel;
			MockServiceBus = mockServiceBus;

			_verifiables = new List<Action<IMessage>>();
		}

		protected IServiceBus MockServiceBus { get; private set; }
		protected TMessageConsumer MessageConsumer { get; private set; }
		protected IKernel Kernel { get; set; }
		protected IMessage ConsumedMessage { get; set; }
		protected Func<IMessage> ExternalConstructMessageAction { get; set; }
		protected Action ConsumeMessageAction { get; set; }

		#region IMessageConsumerTestScenario<TMessageConsumer> Members

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToPublish<T>() where T : IMessage
		{
			Action<IMessage> publish = (mcm) =>
			                           	{
			                           		string verifiable =
			                           			string.Format("The message consumer '{0}' should publish the message '{1}' " +
			                           			              "when the message '{2}' is received.",
			                           			              typeof (TMessageConsumer).Name,
			                           			              typeof (T).Name,
			                           			              mcm.GetImplementationFromProxy().Name);

			                           		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                           		specification.VerifyPublish<T>(verifiable);
			                           	};

			_verifiables.Add(publish);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToPublish<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			var message = CreateMessage<T>();

			if (messageConstructionAction != null)
			{
				messageConstructionAction(message);
			}

			Action<IMessage> publish = (mcm) =>
			                           	{
			                           		string verifiable =
			                           			string.Format("The message consumer '{0}' should publish the message '{1}' " +
			                           			              "when the message '{2}' is received.",
			                           			              typeof (TMessageConsumer).Name,
			                           			              typeof (T).Name,
			                           			              mcm.GetImplementationFromProxy().Name);

			                           		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                           		specification.VerifyPublish(message, verifiable);
			                           	};

			_verifiables.Add(publish);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToPublish<T>() where T : IMessage
		{
			Action<IMessage> publish = (mcm) =>
			                           	{
			                           		string verifiable =
			                           			string.Format("The message consumer '{0}' should not publish the message '{1}' " +
			                           			              "when the message '{2}' is received.",
			                           			              typeof (TMessageConsumer).Name,
			                           			              typeof (T).Name,
			                           			              mcm.GetImplementationFromProxy().Name);

			                           		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                           		specification.VerifyNonPublish<T>(verifiable);
			                           	};

			_verifiables.Add(publish);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToPublish<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			var message = CreateMessage<T>();

			if (messageConstructionAction != null)
			{
				messageConstructionAction(message);
			}

			Action<IMessage> publish = (mcm) =>
			                           	{
			                           		string verifiable =
			                           			string.Format("The message consumer '{0}' should not publish the message '{1}' " +
			                           			              "when the message '{2}' is received.",
			                           			              typeof (TMessageConsumer).Name,
			                           			              typeof (T).Name,
			                           			              mcm.GetImplementationFromProxy().Name);

			                           		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                           		specification.VerifyNonPublish(message, verifiable);
			                           	};

			_verifiables.Add(publish);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToSend<T>() where T : IMessage
		{
			Action<IMessage> send = (mcm) =>
			                        	{
			                        		string verifiable =
			                        			string.Format("The message consumer '{0}' should send the message '{1}' " +
			                        			              "when the message '{2}' is received.",
			                        			              typeof (TMessageConsumer).Name,
			                        			              typeof (T).Name,
			                        			              mcm.GetImplementationFromProxy().Name);

			                        		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                        		specification.VerifySend<T>(verifiable);
			                        	};

			_verifiables.Add(send);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToSend<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			var message = CreateMessage<T>();

			if (messageConstructionAction != null)
			{
				messageConstructionAction(message);
			}

			Action<IMessage> publish = (mcm) =>
			                           	{
			                           		string verifiable =
			                           			string.Format("The message consumer '{0}' should send the message '{1}' " +
			                           			              "when the message '{2}' is received.",
			                           			              typeof (TMessageConsumer).Name,
			                           			              typeof (T).Name,
			                           			              mcm.GetImplementationFromProxy().Name);

			                           		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                           		specification.VerifySend(message, verifiable);
			                           	};

			_verifiables.Add(publish);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSend<T>() where T : IMessage
		{
			Action<IMessage> send = (mcm) =>
			                        	{
			                        		string verifiable =
			                        			string.Format("The message consumer '{0}' should not send the message '{1}' " +
			                        			              "when the message '{2}' is received.",
			                        			              typeof (TMessageConsumer).Name,
			                        			              typeof (T).Name,
			                        			              mcm.GetImplementationFromProxy().Name);

			                        		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                        		specification.VerifyNonSend<T>(verifiable);
			                        	};

			_verifiables.Add(send);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSend<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			var message = CreateMessage<T>();

			if (messageConstructionAction != null)
			{
				messageConstructionAction(message);
			}

			Action<IMessage> publish = (mcm) =>
			                           	{
			                           		string verifiable =
			                           			string.Format("The message consumer '{0}' should not send the message '{1}' " +
			                           			              "when the message '{2}' is received.",
			                           			              typeof (TMessageConsumer).Name,
			                           			              typeof (T).Name,
			                           			              mcm.GetImplementationFromProxy().Name);

			                           		var specification = MockServiceBus as IServiceBusVerificationSpecification;
			                           		specification.VerifyNonSend(message, verifiable);
			                           	};

			_verifiables.Add(publish);
			return this;
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToSendEndpoint<T>(Uri endpoint) where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToSendEndpoint<T>(Uri endpoint,
		                                                                              Action<T> messageConstructionAction)
			where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSendEndpoint<T>(Uri endpoint) where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSendEndpoint<T>(Uri endpoint,
		                                                                                 Action<T> messageConstructionAction)
			where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectReplyWith<T>() where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToReplyWith<T>() where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToRequestTimeout<T>(TimeSpan delayDuration)
			where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> ExpectToRequestTimeout<T>(TimeSpan delayDuration,
		                                                                                Action<T> messageConstructionAction)
			where T : IMessage
		{
			throw new NotImplementedException();
		}

		public IMessageConsumerTestScenario<TMessageConsumer> VerifyAll()
		{
			ConsumeMessageAction();
			_verifiables.ForEach(verify => verify(ConsumedMessage));
			return this;
		}

		#endregion

		private TMessage CreateMessage<TMessage>()
		{
			TMessage message = default(TMessage);

			if (typeof (TMessage).IsInterface)
			{
				message = MockServiceBus.CreateMessage<TMessage>();
			}
			else
			{
				message = (TMessage) typeof (TMessage)
				                     	.Assembly.CreateInstance(typeof (TMessage).FullName);
			}


			return message;
		}
	}
}