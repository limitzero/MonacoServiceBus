namespace Monaco.Bus.Messages.For.Faults
{
	public class NoSubscriptionForMessageFaultMessage : IMessage
	{
		public object Message { get; set; }
	}
}