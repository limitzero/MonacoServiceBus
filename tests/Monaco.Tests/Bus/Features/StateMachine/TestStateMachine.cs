using System;
using Monaco.StateMachine;

namespace Monaco.Tests.Bus.Features.StateMachine
{
	public class TestStateMachineData : IStateMachineData
	{
		public Guid Id { get; set; }
		public string State { get; set; }
		public int Version { get; set; }
	}

	public class TestStateMachine : 
		SagaStateMachine<TestStateMachineData>,
	    StartedBy<TestStartMessage>,
	    OrchestratedBy<TestStartedMessage>
	{
		// states for the saga:
		public State WaitingForSecondMessage { get; set; }

		// these fire the Consume(...) methods on the When() part automatically:
		public Event<TestStartMessage> StartReceived { get; set; }
		public Event<TestStartedMessage> SagaStarted { get; set; }

		public void Consume(TestStartMessage message)
		{
			// called from When() part...
			// do some work, set some variables for later
		}

		public void Consume(TestStartedMessage message)
		{
			// do some work...call when SagaStarted event fires
		}

		public override void Define()
		{
			Name = "The Test Saga!!";

			Initially
				(
					When(StartReceived)
					     	.Publish<TestStartMessage>( (c, m) => m.Id = 1)
					     	.SendToEndpoint<TestStartedMessage>(new Uri("msmq://localhost/my.queue"), (c,m) => { })
					     	.Delay<TestStartedMessage>(TimeSpan.FromSeconds(5), (c, m) => { })
							.Do((m)=> { })
					     	.TransitionTo(WaitingForSecondMessage)
				);

			While(WaitingForSecondMessage,
					When(SagaStarted)
					     	.Publish<TestStartedMessage>( (c,m) => m.Id = 1)
					     	.Reply<TestStartMessage>( (c,m) => m.Id = 1)
					     	.Complete()
				);
		}

	}
}