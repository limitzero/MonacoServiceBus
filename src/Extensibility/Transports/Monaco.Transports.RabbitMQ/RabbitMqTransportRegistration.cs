using System;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;

namespace Monaco.Transports.RabbitMQ
{
	public class RabbitMqTransportRegistration
		: IEndpointTransportRegistration<RabbitMqEndpoint, RabbitMqTransport>
	{
		public IContainer Container { get; set; }

		public string Scheme
		{
			get { return new RabbitMqEndpoint().Scheme; }
		}

		public Exchange Resolve(string endpointName)
		{
			var ep = new RabbitMqEndpoint();
			var uri = ep.BuildUriFromEndpointName(endpointName);
			return this.Resolve(uri);
		}

		public Exchange Resolve(Uri endpoint)
		{
			var ep = new RabbitMqEndpoint();
			ep.Configure(endpoint);

			var transport = new RabbitMqTransport(this.Container, ep);

			return new Exchange(ep, transport);
		}
	}
}