using Monaco.Bus.Internals;

namespace Monaco
{
	/// <summary>
	/// Basic contract for declaring a point for message consumption.
	/// </summary>
	/// <typeparam name="TMessage">Type of message to consume</typeparam>
	public interface Consumes<TMessage> : IConsumer where TMessage : IMessage
	{
		/// <summary>
		/// This will consume the current message and process it accordingly
		/// </summary>
		/// <param name="message">Message to process</param>
		void Consume(TMessage message);
	}
}