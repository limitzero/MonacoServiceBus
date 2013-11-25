using Monaco.Bus.Internals;

namespace Monaco
{
	/// <summary>
	/// Contract for a component that will produce a certain message. This 
	/// is primarily used in conjuction with the scheduler for simple time-based
	/// tasks that execute on a schedule.
	/// </summary>
	/// <typeparam name="TMessage">The message to produce</typeparam>
	public interface Produces<TMessage> : IProducer where TMessage : class, IMessage
	{
		/// <summary>
		/// This will return the message to be consumed on the messaging infrastructure.
		/// </summary>
		/// <returns></returns>
		TMessage Produce();
	}
}