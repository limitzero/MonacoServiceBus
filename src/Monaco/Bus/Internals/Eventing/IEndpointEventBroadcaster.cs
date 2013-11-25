using System;

namespace Monaco.Bus.Internals.Eventing
{
	/// <summary>
	/// Contract for any endpoint subscriber to broadcast events about
	/// messages being processed from the physical transport.
	/// </summary>
	public interface IEndpointEventBroadcaster
	{
		/// <summary>
		/// Event that is triggered when the endpoint receives a message:
		/// </summary>
		event EventHandler<EndpointMessageReceivedEventArgs> EndpointMessageReceivedEvent;
	}
}