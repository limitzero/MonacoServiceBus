namespace Monaco
{
	/// <summary>
	/// Contract to denote the starting message that initiates the beginning  of a long-running process.
	/// </summary>
	/// <typeparam name="TMessage">Message that starts the long running process.</typeparam>
	public interface StartedBy<TMessage> :
		Consumes<TMessage> where TMessage : IMessage
	{
	}
}