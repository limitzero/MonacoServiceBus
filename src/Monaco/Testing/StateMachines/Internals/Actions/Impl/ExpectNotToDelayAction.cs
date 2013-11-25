using System;
using Castle.MicroKernel;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	public class ExpectNotToDelayAction<TMessage, TStateMachine> :
		BaseTestExpectationAction<TStateMachine>
		where TStateMachine : SagaStateMachine
		where TMessage : IMessage
	{
		private readonly TimeSpan _delay;
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectNotToDelayAction(IKernel kernel,
		                              TStateMachine stateMachine,
		                              IServiceBus mockServiceBus,
		                              TimeSpan delay,
		                              IMessage consumedMessage,
		                              Action<TMessage> messageConstructionAction)
			: base(kernel, stateMachine, mockServiceBus, consumedMessage)
		{
			_delay = delay;
			_messageConstructionAction = messageConstructionAction;
		}


		public override Action<IMessage> CreateExpectation()
		{
			var message = CreateMessage<TMessage>();

			Action<IMessage> delayAction = null;

			if (_messageConstructionAction != null)
			{
				_messageConstructionAction(message);
			}

			delayAction = (sm) => CreateNonDelayInvocation(message, sm);

			return delayAction;
		}

		private void CreateNonDelayInvocation(IMessage message, IMessage consumedMessage)
		{
			string verifiable = string.Format(
				"The state machine '{0}' should not publish the message '{1}' " +
				"after the duration of '{2}' when the message '{3}' is received.",
				typeof (TStateMachine).Name,
				typeof (TMessage).Name,
				_delay,
				TryGetImplmentationFromProxiedMessage(consumedMessage).Name);

			var specification = MockServiceBus as IServiceBusVerificationSpecification;
			specification.VerifyNonTimeout(_delay, message, verifiable);
		}
	}
}