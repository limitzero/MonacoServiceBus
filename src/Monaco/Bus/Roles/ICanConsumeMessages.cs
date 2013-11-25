namespace Monaco.Bus.Roles
{
	public interface ICanConsumeMessages
	{
		/// <summary>
		/// This will ask the bus to forcibly call the current message consumers
		/// on the current endpoint to consume (process) the set of messages.
		/// </summary>
		/// <param name="messages"></param>
		void ConsumeMessages(params IMessage[] messages);
	}
}