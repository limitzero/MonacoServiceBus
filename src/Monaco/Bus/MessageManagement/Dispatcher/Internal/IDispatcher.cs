using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals;

namespace Monaco.Bus.MessageManagement.Dispatcher.Internal
{
	/// <summary>
	/// Contract for all instances that than dispatch a message to a component for processing.
	/// </summary>
	public interface IDispatcher
	{
		/// <summary>
		/// This will dispatch a message to the indicated component for processing.
		/// </summary>
		/// <param name="bus">Current instance of the <seealso cref="IServiceBus">service bus</seealso></param>
		/// <param name="handler">Current instance of message handler for processing the message</param>
		/// <param name="envelope"></param>
		/// <exception cref="DispatcherDispatchException"></exception>
		void Dispatch(IServiceBus bus, IConsumer handler, IEnvelope envelope);
	}
}