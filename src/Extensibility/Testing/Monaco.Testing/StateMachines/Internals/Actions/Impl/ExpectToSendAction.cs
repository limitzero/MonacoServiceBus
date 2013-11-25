using System;
using Castle.MicroKernel;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Specifications;
using Monaco.Configuration;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	/// <summary>
	/// Expectation action to send a message for the test condition.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <typeparam name="TStateMachine"></typeparam>
	public class ExpectToSendAction<TMessage, TStateMachine> : BaseTestExpectationAction<TStateMachine>
		where TMessage : IMessage
		where TStateMachine : SagaStateMachine
	{
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectToSendAction(IContainer container, TStateMachine stateMachine,
		                          IServiceBus mockServiceBus,
		                          IMessage consumedMessage,
		                          Action<TMessage> messageConstructionAction)
			: base(container, stateMachine, mockServiceBus, consumedMessage)
		{
			_messageConstructionAction = messageConstructionAction;
		}

		public override Action<IMessage> CreateExpectation()
		{
			Action<IMessage> sendAction;
			var message = CreateMessage<TMessage>();

			if (_messageConstructionAction != null)
			{
				_messageConstructionAction(message);
				sendAction = (sm) => CreateSendExpectation(message, sm);
			}
			else
			{
				sendAction = (sm) => CreateSendExpectation(message, sm, true);
			}

			return sendAction;
		}

		private void CreateSendExpectation(IMessage message, IMessage consumedMessage, bool useGenericInvocation = false)
		{
			string verifiable = string.Format(
				"The state machine '{0}' should send the message '{1}' to the indicated party when the message '{2}' is received.",
				typeof (TStateMachine).Name,
				typeof (TMessage).Name,
				TryGetImplmentationFromProxiedMessage(consumedMessage).Name);

			var specification = MockServiceBus as IServiceBusVerificationSpecification;

			if (useGenericInvocation == false)
			{
				specification.VerifySend(message, verifiable);
			}
			else
			{
				specification.VerifySend<TMessage>(verifiable);
			}
		}
	}
}