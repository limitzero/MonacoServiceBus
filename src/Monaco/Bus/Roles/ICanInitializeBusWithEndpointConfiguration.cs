using System;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role that allows the bus to be configured via a specific endpoint configuration.
	/// </summary>
	public interface ICanInitializeBusWithEndpointConfiguration
	{
		/// <summary>
		/// This will configure the service bus according to the defined components and semantics of the endpoint configuration.
		/// </summary>
		/// <typeparam name="TEndpointConfiguration">Type corresponding to the endpoint configuration for the service bus.</typeparam>
		void ConfiguredWithEndpoint<TEndpointConfiguration>() where TEndpointConfiguration : class, ICanConfigureEndpoint, new();

		/// <summary>
		/// This will configure the service bus according to the defined components and semantics of the endpoint configuration.
		/// </summary>
		/// <param name="endpointConfigurationType">Type corresponding to the endpoint configuration for the service bus.</param>
		void ConfiguredWithEndpoint(Type endpointConfigurationType);
	}
}