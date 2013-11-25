using System;
using Monaco.Bus.Internals;

namespace Monaco.Sagas
{
    /// <summary>
    /// Contract for long-running transactions within the message bus environment.
    /// </summary>
    public interface IStateMachine : IConsumer
    {
        /// <summary>
        /// Gets or sets the instance identifier for the saga instance.
        /// </summary>
        Guid InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the flag to indicate whether or not the process has completed.
        /// </summary>
        bool IsCompleted { get; set; }

		/// <summary>
		/// Gets or sets the flag to indicate whether or not the process is suspended.
		/// </summary>
		bool IsSuspended { get; set; }
    }

}