namespace Monaco.Bus.Messages.For.Faults
{
	/// <summary>
	/// Message that is used to wrap the underlying message that could not be 
	/// associated with a consumer for processing on the endpoint.
	/// </summary>
	public class NoConsumerForMessageFaultMessage : IMessage
	{
		public object Message { get; set; }
	}
}