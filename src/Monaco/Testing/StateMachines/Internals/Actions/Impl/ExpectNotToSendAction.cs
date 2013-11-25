using System;
using Castle.MicroKernel;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	/// <summary>
	/// Expectation action to not send a message for the test condition.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <typeparam name="TStateMachine"></typeparam>
	public class ExpectNotToSendAction<TMessage, TStateMachine> : BaseTestExpectationAction<TStateMachine>
		where TMessage : IMessage
		where TStateMachine : SagaStateMachine
	{
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectNotToSendAction(IKernel kernel, TStateMachine stateMachine, IServiceBus mockServiceBus,
		                             IMessage consumedMessage, Action<TMessage> messageConstructionAction)
			: base(kernel, stateMachine, mockServiceBus, consumedMessage)
		{
			_messageConstructionAction = messageConstructionAction;
		}

		public override Action<IMessage> CreateExpectation()
		{
			var message = CreateMessage<TMessage>();

			Action<IMessage> nonSendAction = null;

			if (_messageConstructionAction != null)
			{
				_messageConstructionAction(message);
				nonSendAction = (sm) => CreateSendExpectation(message, sm);
			}
			else
			{
				nonSendAction = (sm) => CreateSendExpectation(message, sm, true);
			}

			return nonSendAction;
		}

		private void CreateSendExpectation(IMessage message, IMessage consumedMessage, bool useGenericInvocation = false)
		{
			string verifiable = string.Format(
				"The state machine '{0}' should not send the message '{1}' to the indicated party when the message '{2}' is received",
				typeof (TStateMachine).Name,
				typeof (TMessage).Name,
				TryGetImplmentationFromProxiedMessage(consumedMessage).Name);

			var specification = MockServiceBus as IServiceBusVerificationSpecification;

			if (useGenericInvocation == false)
			{
				specification.VerifyNonSend(message, verifiable);
			}
			else
			{
				specification.VerifyNonSend<TMessage>(verifiable);
			}
		}
	}
}