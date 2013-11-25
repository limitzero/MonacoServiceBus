namespace Monaco
{
	/// <summary>
	/// Contract to denoted a subsequent message coming into an existing long-running process.
	/// </summary>
	/// <typeparam name="TMessage">Message that participates in the existing process.</typeparam>
	public interface OrchestratedBy<TMessage> :
		Consumes<TMessage> where TMessage : IMessage
	{
	}
}