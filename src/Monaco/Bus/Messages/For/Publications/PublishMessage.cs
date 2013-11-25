namespace Monaco.Bus.Messages.For.Publications
{
	/// <summary>
	/// Message to pass from local bus instance to 
	/// control or pub/sub endpoint for distributing 
	/// a message that is not reachable on the local 
	/// subscription repository for message routing.
	/// </summary>
	public class PublishMessage : IAdminMessage
	{
		/// <summary>
		/// Gets or sets the endpoint where the request was made to publish 
		/// a message that is not in the endpoints local subscription cache.
		/// </summary>
		public string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the message to publish that is not located in the 
		/// endpoints local subscription cache.
		/// </summary>
		public IMessage Message { get; set; }
	}
}