using System;

namespace Monaco.StateMachine
{
	/// <summary>
	/// Contract that represents the data that is saved between 
	/// calls for a state machine that can be persisted to an external store.
	/// </summary>
	public interface IStateMachineData
	{
		/// <summary>
		/// Gets or sets the instance identifier of the state machine data (done internally by the infrastructure).
		/// </summary>
		Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the current state of the state machine.
		/// </summary>
		string State { get; set; }

		/// <summary>
		/// Gets or sets the version of the state machine (if defined) for 
		/// later version/data merge scenarios.
		/// </summary>
		int Version { get; set; }
	}
}