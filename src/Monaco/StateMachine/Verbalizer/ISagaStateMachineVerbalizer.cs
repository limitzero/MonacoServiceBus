namespace Monaco.StateMachine.Verbalizer
{
	public interface ISagaStateMachineVerbalizer
	{
		string Verbalize<TSagaStateMachine>() where TSagaStateMachine : SagaStateMachine, new();

		string Verbalize<TSagaStateMachine>(TSagaStateMachine sagaStateMachine)
			where TSagaStateMachine : SagaStateMachine;
	}
}