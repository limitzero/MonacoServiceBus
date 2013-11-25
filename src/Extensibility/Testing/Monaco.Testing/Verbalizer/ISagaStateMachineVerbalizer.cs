using System;
using Monaco.StateMachine;

namespace Monaco.Testing.Verbalizer
{
	public interface ISagaStateMachineVerbalizer
	{
		string Verbalize<TSagaStateMachine>() where TSagaStateMachine : SagaStateMachine, new();

		string Verbalize<TSagaStateMachine>(TSagaStateMachine sagaStateMachine)
			where TSagaStateMachine : SagaStateMachine;

		string Verbalize(Type stateMachine);
	}
}