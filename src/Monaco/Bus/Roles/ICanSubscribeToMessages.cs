namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role to allow for messages to be temporarily added to the local service endpoint.
	/// </summary>
	public interface ICanSubscribeToMessages
	{
		/// <summary>
		/// This will instruct the message bus to mark this message as eligible for the service bus
		/// to pick-up for processing and associate a particular component instance
		/// for processing the message.
		/// </summary>
		/// <typeparam name="TMessage">The message to mark as eligble for the service bus to process.</typeparam>
		IDisposableAction Subscribe<TMessage>()
			where TMessage : IMessage;
	}
}