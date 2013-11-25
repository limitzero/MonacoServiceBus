using System;
using System.IO;
using Monaco.StateMachine;
using Monaco.Tests.Bus.Features.StateMachine;
using Rhino.Mocks;
using Xunit;

namespace Monaco.Tests
{
	public class CanUseSagaStateMachineToCreateFSMGraph
	{
		private readonly MockRepository _mocks;
		private readonly RegistrationStateMachine _stateMachine;

		public CanUseSagaStateMachineToCreateFSMGraph()
		{
			_mocks = new MockRepository();
			var bus = _mocks.DynamicMock<IServiceBus>();

			_stateMachine = new RegistrationStateMachine();
			_stateMachine.Bus = bus;
			_stateMachine.Define();
		}

		[Fact]
		public void can_build_finite_state_machine_graph_from_saga_state_machine()
		{
			var image = Path.Combine(@"C:\repositories\Monaco\logs",
				string.Concat(Guid.NewGuid().ToString(), ".dgml"));

			var visualizer = new SagaStateMachineVisualizer();

			var stream = visualizer.Visualize<SagaStateMachineTests.LocalStateMachine>();

			// write the image to the file system:
			using (FileStream fileStream = new FileStream(image, FileMode.Create, FileAccess.Write))
			{
				fileStream.Write(stream.ToArray(), 0, stream.ToArray().Length);
			}
		}

		[Fact]
		public void can_build_finite_state_machine_graph_from_saga_state_machine_and_indicate_current_state_with_asterisk()
		{
			var image = Path.Combine(@"C:\repositories\Monaco\logs", 
				string.Concat(Guid.NewGuid().ToString(), "_asterisk.bmp"));

			var visualizer = new SagaStateMachineVisualizer();
			_stateMachine.CurrentState = new State("AnotherState");
			var stream = visualizer.Visualize(_stateMachine);
			
			// write the image to the file system:
			using (FileStream fileStream = new FileStream(image, FileMode.Create, FileAccess.Write))
			{
				fileStream.Write(stream.ToArray(), 0, stream.ToArray().Length);
			}
		}
	}

	public class RequestForLoginMessage : IMessage
	{
		public Guid CorrelationId { get; set; }
	}

	public class ConfirmationReceivedMessage : IMessage
	{
		public Guid CorrelationId { get; set; }
	}

	public class RegistrationStateMachineData  : IStateMachineData
	{
		public Guid Id { get; set; }
		public string State { get; set; }
		public int Version { get; set; }
	}

	public class RegistrationStateMachine : 
		SagaStateMachine<RegistrationStateMachineData>,
		StartedBy<RequestForLoginMessage>,
		OrchestratedBy<ConfirmationReceivedMessage>
	{
		public Event<RequestForLoginMessage> LoginRequested { get; set; }
		public Event<ConfirmationReceivedMessage> ConfirmationReceived { get; set; }

		public State WaitingForAuthorization { get; set; }
		public State AnotherState { get; set; }

		public override void Define()
		{
			Initially(
				When(LoginRequested)
				.TransitionTo(WaitingForAuthorization)
				);

			While(WaitingForAuthorization,
				When(ConfirmationReceived)
					.Complete()
				);

			While(WaitingForAuthorization,
				When(ConfirmationReceived)
					.Do((m) => { })
					.TransitionTo(AnotherState)
					);

		}

		public void Consume(RequestForLoginMessage message)
		{

		}

		public void Consume(ConfirmationReceivedMessage message)
		{
			throw new NotImplementedException();
		}
	}
}