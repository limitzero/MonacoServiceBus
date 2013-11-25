using System;
using System.Collections.Generic;
using Monaco.Sagas;
using Monaco.Sagas.StateMachine;

namespace Monaco.Persistance.Repositories
{
	/// <summary>
	/// Contract for retrieving and saving saga data to the persistance store.
	/// </summary>
	public interface IStateMachineDataRepository<TStateMachineData>
		where TStateMachineData : class, IStateMachineData, new()
	{
		IEnumerable<TStateMachineData> FindAll();

		/// <summary>
		/// Retrieves an instance of a saga's data from the repository and materializes
		/// the saga instance with the data.
		/// </summary>
		/// <param name="instanceId"></param>
		/// <returns></returns>
		TStateMachineData Find(Guid instanceId);

		/// <summary>
		/// Persists the saga's data to the repository.
		/// </summary>
		/// <param name="stateMachineData"></param>
		void Save(TStateMachineData stateMachineData);

		/// <summary>
		/// Removes an instance of the saga from the repository.
		/// </summary>
		/// <param name="stateMachineData">Instance of the saga data to remove</param>
		void Remove(TStateMachineData stateMachineData);

		/// <summary>
		/// Removes an instance of the saga from the repository.
		/// </summary>
		/// <param name="instanceId">Instance identifier of the saga to remove</param>
		void Remove(Guid instanceId);
	}
}