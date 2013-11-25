namespace Monaco.Sagas.StateMachine
{
	/// <summary>
	/// An event that is correlated to the arrival of a message to the message consumer.
	/// </summary>
	/// <typeparam name="TMessage">Concrete message that the consumer will accept and trigger an event for.</typeparam>
	public class Event<TMessage> where TMessage : IMessage
	{
	
	}
}