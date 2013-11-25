using System;
using System.Collections.Generic;
using Monaco.Persistance.Repositories;
using Monaco.Sagas;

namespace Monaco.Persistance.StateMachines
{
	/// <summary>
	/// Abstract repository for implementing storage of saga state machine data to storage medium.
	/// </summary>
	/// <typeparam name="TStateMachineData"></typeparam>
	public abstract class BaseStateMachineDataRepository<TStateMachineData> : 
		IStateMachineDataRepository<TStateMachineData>
		where TStateMachineData : class, IStateMachineData, new()
	{
		public abstract IEnumerable<TStateMachineData> FindAll();
	
		public TStateMachineData Find(Guid instanceId)
		{
			// let the custom-defined routine find the saga state machine data:
			TStateMachineData stateMachineData = this.DoFindStateMachineData(instanceId);
			return stateMachineData;
		}

		/// <summary>
		/// This will extract the state machine data from the repository for a given instance identifier 
		/// (to be carried out by implemented class).
		/// </summary>
		/// <param name="instanceId"></param>
		/// <returns></returns>
		public abstract TStateMachineData DoFindStateMachineData(Guid instanceId);

		public abstract void Save(TStateMachineData stateMachine);

		public abstract void Remove(Guid instanceId);
		
		public virtual void Remove(TStateMachineData stateMachine)
		{
			this.Remove(stateMachine.CorrelationId);
		}
	}
}