using System;
using System.Collections.Generic;
using Monaco.Bus.Services.Timeout.Messages.Commands;

namespace Monaco.Extensibility.Storage.Timeouts
{
    /// <summary>
    /// Contract for persisting timeouts to the data store.
    /// </summary>
    public interface ITimeoutsRepository
    {

		/// <summary>
		/// This will return the listing of  all timeouts in the data store for a given endpoint.
		/// </summary>
		/// <returns></returns>
		ICollection<ScheduleTimeout> FindAll(string endpoint);

        /// <summary>
        /// This will return the listing of  all timeouts in the data store.
        /// </summary>
        /// <returns></returns>
        ICollection<ScheduleTimeout> FindAll();

        /// <summary>
        /// This will add the scheduled timeout to the data store.
        /// </summary>
        /// <param name="timeout">Timeout message to register for later delivery.</param>
        void Add(ScheduleTimeout timeout);

        /// <summary>
        /// This will remove the timeout instance from the data store
        /// (essentially a "cancel").
        /// </summary>
        /// <param name="timeout">Timeout message to remove.</param>
        void Remove(ScheduleTimeout timeout);

        /// <summary>
        /// This will remove the timeout instance from the data store by identifier (essentially 
        /// a "cancel").
        /// </summary>
        /// <param name="timeoutId">Identifier of the timeout message.</param>
        void Remove(Guid timeoutId);

    	/// <summary>
    	/// This will remove a set of timeouts that have been issued by a requestor (namely a state machine)
    	/// when the process has been completed.
    	/// </summary>
    	/// <param name="requestorId"></param>
    	void RemoveRequestedTimeouts(Guid requestorId);
    }
}