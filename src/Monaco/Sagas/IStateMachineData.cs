using System;

namespace Monaco.Sagas
{
    /// <summary>
    /// Contract that represents the data that is saved between 
    /// calls for a state machine that can be persisted to an external store.
    /// </summary>
    public interface IStateMachineData : IMessage, 
		CorrelatedBy<Guid>
    {
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