using System;
using Monaco.Bus.Agents.Scheduler.EventArgs;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Agents.Scheduler
{
	public interface IScheduledItem : IStartable, INotificationEventBroadcaster
	{
		string Name { get; set; }
		bool HaltOnError { get; set; }
		bool ForceStart { get; set; }
		IScheduledTask Task { get; set; }
		event EventHandler<ScheduledItemMessageCreatedEventArgs> ScheduledItemMessageCreated;
		event EventHandler<ScheduledItemErrorEventArgs> ScheduledItemError;
	}
}