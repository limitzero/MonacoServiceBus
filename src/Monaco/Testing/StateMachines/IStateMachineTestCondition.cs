using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Monaco.StateMachine;

namespace Monaco.Testing.StateMachines
{
	public interface IStateMachineTestCondition<TStateMachine> 
		where TStateMachine : SagaStateMachine
	{
		ICollection<Action> PostConditions { get; }

		IStateMachineTestCondition<TStateMachine> ExpectToPublish<T>() where T : IMessage;
		IStateMachineTestCondition<TStateMachine> ExpectToPublish<T>(Action<T> messageCreationAction) where T : IMessage;

		IStateMachineTestCondition<TStateMachine> ExpectToSend<T>() where T : IMessage;
		IStateMachineTestCondition<TStateMachine> ExpectToSend<T>(Action<T> messageCreationAction) where T : IMessage;

		IStateMachineTestCondition<TStateMachine> ExpectToDelay<T>(TimeSpan delayDuration) where T : IMessage;

		IStateMachineTestCondition<TStateMachine> ExpectToDelay<T>(TimeSpan delayDuration, Action<T> messageConstructionAction)
			where T : IMessage;

		IStateMachineTestCondition<TStateMachine> ExpectToTransitionToState(
			Expression<Func<TStateMachine, State>> expectedState);

		IStateMachineTestCondition<TStateMachine> ExpectToComplete();
	}
}