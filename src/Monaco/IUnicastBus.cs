using System;
using Monaco.Bus.Roles;
using Monaco.Endpoint;

namespace Monaco
{
	/// <summary>
	/// Contract for a service bus that will only support message transmissions and can not 
	/// be started/stopped for receiving messages from the underlying choice of transport (half-duplex).
	/// The uni-cast bus is in most instances the control bus for the service bus.
	/// </summary>
	public interface IUnicastBus :
		ICanFindComponents,
		ICanPublishMessages,
		ICanSendMessages,
		ICanReplyToMessages,
		ICanHandleMessageLater,
		ICanEnlistInstanceConsumers,
		ICanConsumeMessages,
		ICanInitializeBusWithEndpointConfiguration
	{
		/// <summary>
		/// Gets the endpoint of the service bus.
		/// </summary>
		IEndpoint Endpoint { get;  }

		/// <summary>
		/// This will create an concrete message from an interface-based message definition.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to create</typeparam>
		/// <returns>
		/// Concrete type representing message, exception if type is not an interface.
		/// </returns>
		TMessage CreateMessage<TMessage>();

		/// <summary>
		/// This will create an concrete message from an interface-based message definition
		/// and pass in the arguments for creation via a delegate function.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to create</typeparam>
		/// <returns>
		/// Concrete type representing message, exception if type is not an interface.
		/// </returns>
		TMessage CreateMessage<TMessage>(Action<TMessage> create);

		/// <summary>
		/// This will redirect the endpoint for the bus:
		/// </summary>
		/// <param name="endpoint"></param>
		void SetEndpoint(IEndpoint endpoint);
	}
}