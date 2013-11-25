using System;
using Castle.MicroKernel;
using Monaco.StateMachine;

namespace Monaco.Testing.StateMachines.Internals.Actions.Impl
{
	public class ExpectToRequestTimeoutAction<TMessage, TStateMachine> :
		BaseTestExpectationAction<TStateMachine>
		where TStateMachine : SagaStateMachine
		where TMessage : IMessage
	{
		private readonly TimeSpan _delay;
		private readonly Action<TMessage> _messageConstructionAction;

		public ExpectToRequestTimeoutAction(IKernel kernel,
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

			string verifiable = string.Format(
				"The state machine '{0}' should publish the message '{1}' after the duration/timeout " +
				"period of '{2}'",
				typeof (TStateMachine).Name,
				typeof (TMessage).Name,
				_delay);

			Action<IMessage> delayAction = null;

			//this.MockServiceBus.Setup(bus => bus.Find<ITimeoutsRepository>())
			//    .Returns(this.Kernel.Resolve<ITimeoutsRepository>())
			//    .Verifiable(verifiable);

			return delayAction;
		}
	}
}