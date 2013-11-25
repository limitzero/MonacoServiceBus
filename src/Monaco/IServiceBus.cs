using System;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Eventing;
using Monaco.Endpoint.Impl.Control;

namespace Monaco
{
	/// <summary>
	/// Contract for a service bus that participates in full-duplex messaging (sending and recieving).
	/// </summary>
	public interface IServiceBus :
		IUnicastBus,
		IStartable,
		INotificationEventBroadcaster
	{
		/// <summary>
		/// Event that is fired when the bus is started.
		/// </summary>
		Action<string> OnStart { get; set; }

		/// <summary>
		/// Event that is fired when the bus is stopped.
		/// </summary>
		Action<string> OnStop { get; set; }

		/// <summary>
		/// This will enqueue a request to be sent using the BeginXXX/EndXXX pattern for managing
		/// the <seealso cref="IAsyncResult"/> object or to manage a semi-sychronous request reply 
		/// scenario where the calling code needs to directly get the response to a request.
		/// </summary>
		/// <returns></returns>
		IServiceAsyncRequest EnqueueRequest();

		/// <summary>
		/// This will get the control endpoint (if defined) where the local service bus will report up to and receive
		/// control messages for maintenance and reporting.
		/// </summary>
		/// <returns></returns>
		IControlEndpoint GetControlEndpoint();

		/// <summary>
		/// This will forcibly complete an asynchrous request to the service bus and return a message (if necessary)
		/// </summary>
		/// <typeparam name="TMessage">Requested message to be responded to asynchronously</typeparam>
		/// <param name="response"></param>
		void CompleteAsyncRequestFor<TMessage>(IMessage response = null) where TMessage : class, IMessage, new();
	}
}