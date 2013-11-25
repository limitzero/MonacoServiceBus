namespace Monaco.Bus.Messages.For.Control
{
	/// <summary>
	/// Message to pass from local bus instance to 
	/// control or pub/sub endpoint for distributing 
	/// a message that is not reachable on the local 
	/// subscription repository for message routing.
	/// </summary>
	public class DeliverMessage : IAdminMessage
	{
		public string Endpoint { get; set; }
		public IMessage Message { get; set; }
	}
}