using System;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;

namespace Monaco.Transports.Msmq
{
	public class MsmqRegistration : 
		IEndpointTransportRegistration<MsmqEndpoint, MsmqTransport>
	{
		public IContainer Container { get; set; }

		public string Scheme
		{
			get { return new MsmqEndpoint().Scheme; }
		}

		public Exchange Resolve(Uri endpoint)
		{
			var ep = new MsmqEndpoint();
			ep.Configure(endpoint);

			var transport = new MsmqTransport(ep);

			return new Exchange(ep, transport);
		}
	}
}