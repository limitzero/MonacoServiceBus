using System;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.StateMachine;

namespace Monaco.Testing.StateMachines.Internals.Actions
{
	public abstract class BaseTestExpectationAction<TStateMachine> 
		where TStateMachine : SagaStateMachine
	{
		protected BaseTestExpectationAction(IContainer container,
		                                    TStateMachine stateMachine,
		                                    IServiceBus mockServiceBus,
		                                    IMessage consumedMessage)
		{
			Container = container;
			StateMachine = stateMachine;
			MockServiceBus = mockServiceBus;
			ConsumedMessage = consumedMessage;
		}

		protected IMessage ConsumedMessage { get; set; }
		protected IContainer Container { get; private set; }
		protected TStateMachine StateMachine { get; private set; }
		protected IServiceBus MockServiceBus { get; private set; }

		public abstract Action<IMessage> CreateExpectation();

		protected TMessage CreateMessage<TMessage>()
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

		protected static Type TryGetImplmentationFromProxiedMessage(IMessage message)
		{
			Type result = message.GetType();

			if (message.GetType().Name.Contains("Proxy"))
			{
				// parent interface for proxied message:
				result = message.GetType().GetInterfaces()[0];
			}

			return result;
		}
	}
}