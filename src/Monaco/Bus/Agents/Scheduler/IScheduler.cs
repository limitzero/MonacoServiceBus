using System.Collections.Generic;
using Monaco.Bus.Agents.Scheduler.Tasks.Configuration;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Agents.Scheduler
{
	/// <summary>
	/// Contract for the task scheduler that will execute a set of registered tasks for scheduled execution.
	/// </summary>
	public interface IScheduler : IStartable, INotificationEventBroadcaster, IErrorEventBroadcaster, IMessageReceiver
	{
		/// <summary>
		/// (Read-Write). The collection of scheduled items that are set for execution.
		/// </summary>
		ICollection<IScheduledItem> RegisteredItems { get; set; }

		/// <summary>
		/// This will register an item in the scheduler for execution.
		/// </summary>
		/// <param name="item"></param>
		void RegisterItem(IScheduledItem item);

		/// <summary>
		/// This will create an item to be scheduled in the scheduler for execution at a particular interval.
		/// </summary>
		/// <param name="taskName"></param>
		/// <param name="interval"></param>
		/// <param name="task"></param>
		/// <param name="taskMethod"></param>
		/// <param name="haltOnError"></param>
		/// <param name="forceStart"></param>
		void CreateScheduledItem(string taskName, string interval, object task, string taskMethod, bool haltOnError,
		                         bool forceStart);

		/// <summary>
		/// This will create an item to be scheduled in the scheduler for execution at a particular interval.
		/// </summary>
		void CreateFromConfiguration(ITaskConfiguration configuration);
	}
}