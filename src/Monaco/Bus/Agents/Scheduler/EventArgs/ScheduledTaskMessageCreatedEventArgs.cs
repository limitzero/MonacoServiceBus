namespace Monaco.Bus.Agents.Scheduler.EventArgs
{
	public class ScheduledTaskMessageCreatedEventArgs : System.EventArgs
	{
		public ScheduledTaskMessageCreatedEventArgs(IScheduledTask task, IMessage message)
		{
			Task = task;
			Message = message;
		}

		public IScheduledTask Task { get; private set; }
		public IMessage Message { get; private set; }
	}
}