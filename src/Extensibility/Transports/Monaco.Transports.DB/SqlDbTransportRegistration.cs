using System;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;

namespace Monaco.Transports.DB
{
	public class SqlDbTransportRegistration : 
		IEndpointTransportRegistration<SqlDbEndpoint, SqlDbTransport>
	{
		public IContainer Container { get; set; }

		public string Scheme
		{
			get { return new SqlDbEndpoint().Scheme; }
		}

		public Exchange Resolve(string endpointName)
		{
			var ep = new SqlDbEndpoint();
			var uri = ep.BuildUriFromEndpointName(endpointName);
			return this.Resolve(uri);
		}

		public Exchange Resolve(Uri endpoint)
		{
			var ep = new SqlDbEndpoint();
			ep.Configure(endpoint);

			var transport = new SqlDbTransport(this.Container, ep);

			return new Exchange(ep, transport);
		}
	}
}