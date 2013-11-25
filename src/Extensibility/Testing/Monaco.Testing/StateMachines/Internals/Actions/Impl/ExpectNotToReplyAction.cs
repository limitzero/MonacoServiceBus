using System;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	public class ExpectNotToReplyAction<TMessage, TStateMachine> :
		BaseTestExpectationAction<TStateMachine>
		where TStateMachine : SagaStateMachine
		where TMessage : IMessage
	{
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectNotToReplyAction(IContainer container,
		                              TStateMachine stateMachine,
		                              IServiceBus mockServiceBus,
		                              IMessage consumedMessage,
		                              Action<TMessage> messageConstructionAction)
			: base(container, stateMachine, mockServiceBus, consumedMessage)
		{
			_messageConstructionAction = messageConstructionAction;
		}

		public override Action<IMessage> CreateExpectation()
		{
			var message = CreateMessage<TMessage>();

			if (_messageConstructionAction != null)
			{
				_messageConstructionAction(message);
			}

			Action<IMessage> replyAction = (sm) => CreateReplyExpectation(message, sm);

			return replyAction;
		}

		private void CreateReplyExpectation(IMessage message, IMessage consumedMessage)
		{
			string verifiable = string.Format("The state machine '{0}' should not reply with the message '{1}' " +
			                                  "to the indicated party when the message '{2}' is received.",
			                                  typeof (TStateMachine).Name,
			                                  typeof (TMessage).Name,
			                                  TryGetImplmentationFromProxiedMessage(consumedMessage).Name);

			var specification = MockServiceBus as IServiceBusVerificationSpecification;

			if (specification != null)
				specification.VerifyNonReply(message, verifiable);
		}
	}
}