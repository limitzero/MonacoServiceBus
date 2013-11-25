using System;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Transport;

namespace Monaco.Endpoint.Registrations
{
	/// <summary>
	/// Interface used for run-time buliding of registration.
	/// </summary>
	public interface IEndpointTransportRegistration
	{
		/// <summary>
		/// Gets the addressing scheme for the endpoint uri denoting where 
		/// the messages will be located for receipt or delivery
		/// </summary>
		string Scheme { get; }

		/// <summary>
		/// This will take the current endpoint name and 
		/// return back the transprt that matches the endpoint uri 
		/// definition for message receipt or delivery.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		Exchange Resolve(string endpointName);

		/// <summary>
		/// This will take the current endpoint uri definition and 
		/// return back the transprt that matches the endpoint uri 
		/// definition for message receipt or delivery.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		Exchange Resolve(Uri endpoint);

		/// <summary>
		/// Gets or sets access to the underlying container for resolving dependencies.
		/// </summary>
		IContainer Container { get; set; }
	}

	/// <summary>
	/// Interface used for design-time registration of endpoint definition to transport medium.
	/// </summary>
	/// <typeparam name="TEndpoint"></typeparam>
	/// <typeparam name="TTransport"></typeparam>
	public interface IEndpointTransportRegistration<TEndpoint, TTransport> : IEndpointTransportRegistration
		where TEndpoint : class, IEndpoint, new()
		where TTransport : class, ITransport
	{
	}
}