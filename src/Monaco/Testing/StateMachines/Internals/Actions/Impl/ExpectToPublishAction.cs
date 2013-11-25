using System;
using Castle.MicroKernel;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	/// <summary>
	/// Expectation action to publish a message for the test condition.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <typeparam name="TStateMachine"></typeparam>
	public class ExpectToPublishAction<TMessage, TStateMachine> : BaseTestExpectationAction<TStateMachine>
		where TMessage : IMessage
		where TStateMachine : SagaStateMachine
	{
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectToPublishAction(IKernel kernel, TStateMachine stateMachine,
		                             IServiceBus mockServiceBus,
		                             IMessage consumedMessage,
		                             Action<TMessage> messageConstructionAction)
			: base(kernel, stateMachine, mockServiceBus, consumedMessage)
		{
			_messageConstructionAction = messageConstructionAction;
		}

		public override Action<IMessage> CreateExpectation()
		{
			Action<IMessage> publishAction;
			var message = CreateMessage<TMessage>();

			if (_messageConstructionAction != null)
			{
				_messageConstructionAction(message);
				publishAction = (sm) => CreatePublishExpectation(message, sm);
			}
			else
			{
				publishAction = (sm) => CreatePublishExpectation(message, sm, true);
			}

			return publishAction;
		}

		private void CreatePublishExpectation(IMessage message,
		                                      IMessage consumedMessage,
		                                      bool useGenericInvocation = false)
		{
			string verifiable = string.Format("The state machine '{0}' failed to publish the message '{1}' " +
			                                  "when the message '{2}' was received.",
			                                  typeof (TStateMachine).Name,
			                                  typeof (TMessage).Name,
			                                  TryGetImplmentationFromProxiedMessage(consumedMessage).Name);

			var specification = MockServiceBus as IServiceBusVerificationSpecification;

			if (useGenericInvocation == false)
			{
				specification.VerifyPublish(message, verifiable);
			}
			else
			{
				specification.VerifyPublish<TMessage>(verifiable);
			}
		}
	}
}