using System;
using System.Linq.Expressions;
using Monaco.Configuration.Endpoint;

namespace Monaco.Configuration
{
	public interface IConfiguration
	{
		/// <summary>
		/// Gets the underlying container for component registration and resolution.
		/// </summary>
		IContainer Container { get; }

		/// <summary>
		/// Gets the name that is set for the endpoint where all messages and storage components will be resolved to
		/// </summary>
		string EndpointName { get; }

		/// <summary>
		/// Defines the singular endpoint name that will be used for durable storage components.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		IConfiguration WithEndpointName(string name);

		/// <summary>
		/// Defines the underlying container implementation for the service bus.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		IConfiguration WithContainer(Expression<Func<IContainerConfiguration, IContainerConfiguration>> configuration);

		/// <summary>
		/// Defines the storage options for messages on the service bus.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		IConfiguration WithStorage(Expression<Func<IStorageConfiguration, IStorageConfiguration>> configuration);

		/// <summary>
		/// Defines the transport options for messages on the service bus.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		IConfiguration WithTransport(Expression<Func<ITransportConfiguration, ITransportConfiguration>> configuration);

		/// <summary>
		/// Defines the semantics to be used on the local endpoint for messages and message handlers.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		IConfiguration WithEndpoint(Expression<Func<IEndpointConfiguration, IEndpointConfiguration>> configuration);
	}
}