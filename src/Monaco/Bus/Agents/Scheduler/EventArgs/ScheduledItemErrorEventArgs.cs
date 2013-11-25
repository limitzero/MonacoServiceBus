using System;

namespace Monaco.Bus.Agents.Scheduler.EventArgs
{
	public class ScheduledItemErrorEventArgs : System.EventArgs
	{
		public ScheduledItemErrorEventArgs(IScheduledItem scheduledItem, Exception exception)
		{
			ScheduledItem = scheduledItem;
			Exception = exception;
		}

		public IScheduledItem ScheduledItem { get; private set; }
		public Exception Exception { get; private set; }
	}
}