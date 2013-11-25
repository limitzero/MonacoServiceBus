using Monaco.StateMachine;

namespace Monaco.Extensibility.Storage.StateMachines
{
	/// <summary>
	/// Contract needed by container for registering all instances.
	/// </summary>
	public interface IStateMachineDataMerger
	{
	}

	/// <summary>
	/// Contract for merging state machine data in the event that a version collision happens:
	/// </summary>
	/// <typeparam name="TStateMachineData"></typeparam>
	/// <typeparam name="TStateMachineMessage"></typeparam>
	public interface IStateMachineDataMerger<TStateMachineData, TStateMachineMessage> : IStateMachineDataMerger
		where TStateMachineData : class, IStateMachineData
		where TStateMachineMessage : IMessage
	{
		/// <summary>
		/// This will merge the current state machine data with the retrieved 
		/// machine data retreived from storage in the case where the version numbers 
		/// do not match.
		/// </summary>
		/// <param name="currentStateMachineData">Instance created internally without data that has the updated version number.</param>
		/// <param name="retreivedStateMachineData">Instance retreived from the persistance store that has the older version number.</param>
		/// <param name="stateMachineMessage">Current message, if needed for merging data</param>
		/// <returns></returns>
		TStateMachineData Merge(TStateMachineData currentStateMachineData,
		                        TStateMachineData retreivedStateMachineData,
		                        TStateMachineMessage stateMachineMessage);
	}
}