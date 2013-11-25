namespace Monaco.StateMachine
{
	/// <summary>
	/// An event that is correlated to the arrival of a message to the state machine for processing.
	/// </summary>
	/// <typeparam name="TMessage">Message that the state machine will accept and trigger a series of processing actions.</typeparam>
	public class Event<TMessage> where TMessage : IMessage
	{
	}
}