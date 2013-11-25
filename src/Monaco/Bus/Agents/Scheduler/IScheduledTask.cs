using System;
using Monaco.Bus.Agents.Scheduler.EventArgs;
using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Agents.Scheduler
{
	/// <summary>
	/// Contract for a scheduled task.
	/// </summary>
	public interface IScheduledTask : INotificationEventBroadcaster, IErrorEventBroadcaster
	{
		/// <summary>
		/// (Read-Write). The interval, in seconds, that the method on the component should be polled.
		/// </summary>
		string Interval { get; set; }

		event EventHandler<ScheduledTaskMessageCreatedEventArgs> ScheduledTaskMessageCreated;

		/// <summary>
		/// This will execute the scheduled task.
		/// </summary>
		void Execute();
	}
}