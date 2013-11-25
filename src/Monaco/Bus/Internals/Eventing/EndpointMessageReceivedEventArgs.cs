using System;

namespace Monaco.Bus.Internals.Eventing
{
	/// <summary>
	/// Event arguements passed to event subscriber when a message is received from an endpoint.
	/// </summary>
	public class EndpointMessageReceivedEventArgs : EventArgs
	{
		public EndpointMessageReceivedEventArgs(string endpoint, IMessage message)
		{
			Endpoint = endpoint;
			Message = message;
		}

		public string Endpoint { get; private set; }
		public IMessage Message { get; private set; }
	}
}