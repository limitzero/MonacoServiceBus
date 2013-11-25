using Monaco.Exceptions;

namespace Monaco.Internals.Dispatcher
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
        /// <param name="message">Current message to be processed by the handler</param>
        /// <exception cref="DispatcherDispatchException"></exception>
        void Dispatch(IServiceBus bus, IConsumer handler, IMessage message);
    }
}