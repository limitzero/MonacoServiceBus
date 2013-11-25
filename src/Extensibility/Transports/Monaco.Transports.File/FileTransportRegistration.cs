using System;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;

namespace Monaco.Transports.File
{
	public class FileTransportRegistration : 
		IEndpointTransportRegistration<FileEndpoint, FileTransport>
	{
		public IContainer Container { get; set; }

		public string Scheme
		{
			get { return new FileEndpoint().Scheme; }
		}

		public Exchange Resolve(string endpointName)
		{
			var ep = new FileEndpoint();
			var uri = ep.BuildUriFromEndpointName(endpointName);
			return this.Resolve(uri);
		}

		public Exchange Resolve(Uri endpoint)
		{
			var ep = new FileEndpoint();
			ep.Configure(endpoint);

			var transport = new FileTransport(ep);

			return new Exchange(ep, transport);
		}
	}
}