using System;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	/// <summary>
	/// Expectation action to publish a message for the test condition.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <typeparam name="TStateMachine"></typeparam>
	public class ExpectNotToPublishAction<TMessage, TStateMachine> : BaseTestExpectationAction<TStateMachine>
		where TMessage : IMessage
		where TStateMachine : SagaStateMachine
	{
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectNotToPublishAction(IContainer container,
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

			Action<IMessage> nonPublishAction = null;

			if (_messageConstructionAction != null)
			{
				_messageConstructionAction(message);
				nonPublishAction = (sm) => CreatePublishInvocation(message, sm);
			}
			else
			{
				nonPublishAction = (sm) => CreatePublishInvocation(message, sm, true);
			}

			return nonPublishAction;
		}

		private void CreatePublishInvocation(IMessage message,
		                                     IMessage consumeMessage,
		                                     bool useGenericInvocation = false)
		{
			string verifiable = string.Format("The state machine '{0}' was expected to not publish the " +
			                                  "message '{1}' but actually published it when the message '{2}' was received.",
			                                  typeof (TStateMachine).Name,
			                                  typeof (TMessage).Name,
			                                  TryGetImplmentationFromProxiedMessage(consumeMessage).Name);

			var specification = MockServiceBus as IServiceBusVerificationSpecification;

			if (useGenericInvocation == false)
			{
				specification.VerifyNonPublish(message, verifiable);
			}
			else
			{
				specification.VerifyNonPublish<TMessage>(verifiable);
			}
		}
	}
}