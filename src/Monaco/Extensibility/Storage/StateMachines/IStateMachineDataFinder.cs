using Monaco.StateMachine;

namespace Monaco.Extensibility.Storage.StateMachines
{
	public interface IStateMachineDataFinder
	{
	}

	/// <summary>
	/// Custom state machine data finder to retrieve state machine data from the persistance 
	/// store by advanced criteria.
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="TMessage"></typeparam>
	public interface IStateMachineDataFinder<TData, TMessage>
		: IStateMachineDataFinder
		where TData : class, IStateMachineData
		where TMessage : IMessage
	{
		TData Find(TMessage message);
	}
}