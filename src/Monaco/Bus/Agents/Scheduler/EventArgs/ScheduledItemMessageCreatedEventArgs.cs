namespace Monaco.Bus.Agents.Scheduler.EventArgs
{
	public class ScheduledItemMessageCreatedEventArgs : System.EventArgs
	{
		public ScheduledItemMessageCreatedEventArgs(IMessage message)
		{
			Message = message;
		}

		public IMessage Message { get; private set; }
	}
}