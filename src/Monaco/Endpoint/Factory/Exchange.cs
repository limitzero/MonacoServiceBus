using Monaco.Transport;

namespace Monaco.Endpoint.Factory
{
	/// <summary>
	/// This is the grouping of the transport (mechanism to send and retrieve messages) to the endpoint (the physical point
	/// where the messages will reside). The exchange is built by the endpoint factory for on-demand access to an implementation 
	/// of a transport defined by the location addressing scheme.
	/// </summary>
	public class Exchange
	{
		public Exchange(IEndpoint endpoint, ITransport transport)
		{
			Endpoint = endpoint;
			Transport = transport;
		}

		/// <summary>
		/// Gets the endpoint that defines where the messages will reside.
		/// </summary>
		public IEndpoint Endpoint { get; private set; }

		/// <summary>
		/// Gets the transport that defines how to move messages to/from the endpoint.
		/// </summary>
		public ITransport Transport { get; private set; }
	}
}