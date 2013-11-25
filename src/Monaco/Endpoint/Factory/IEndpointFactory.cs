using System;
using Monaco.Endpoint.Registrations;

namespace Monaco.Endpoint.Factory
{
	/// <summary>
	/// Factory builder that creates an <seealso cref="Exchange">"exchange"</seealso> which is the matching of
	/// the appropriate transport to the endpoint definition.
	/// </summary>
	public interface IEndpointFactory : IDisposable
	{
		/// <summary>
		/// This will return a transport that is matched to the endpoint uri 
		/// semantics with the endpoint name serving as the site and the current 
		/// server name serving as the host (ex: {protocol}://{server}/{endpoint name}
		/// </summary>
		/// <param name="endpointName"></param>
		/// <returns></returns>
		Exchange Build(string endpointName);

		/// <summary>
		/// This will return a transport that is matched to the endpoint uri 
		/// addressing scheme for message receipt or delivery (also known as an "exchange").
		/// </summary>
		/// <param name="endpoint">Uri describing the endpoint</param>
		/// <returns></returns>
		Exchange Build(Uri endpoint);

		/// <summary>
		/// This will register a definition of endpoint to transport 
		/// implementations for the factory to build on demand 
		/// for delivering or receiving messages.
		/// </summary>
		/// <param name="endpointRegistration"></param>
		void Register(IEndpointTransportRegistration endpointRegistration);
	}
}