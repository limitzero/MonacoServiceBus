using System;
using Castle.MicroKernel;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Registrations;

namespace Monaco.Transport.Virtual
{
	/// <summary>
	/// Registration of the virtual or in-memory endpoint 
	/// and transport medium. Addressing scheme: vm://{your unique location name}
	/// </summary>
	public class VirtualEndpointTransportRegistration :
		IEndpointTransportRegistration<VirtualEndpoint, VirtualTransport>
	{
		#region IEndpointTransportRegistration<VirtualEndpoint,VirtualTransport> Members

		public IContainer Container { get; set; }

		public string Scheme
		{
			get { return new VirtualEndpoint().Scheme; }
		}

		public Exchange Resolve(string endpointName)
		{
			var ep = new VirtualEndpoint();
			var uri = ep.BuildUriFromEndpointName(endpointName);
			return this.Resolve(uri);
		}

		public Exchange Resolve(Uri endpoint)
		{
			var ep = new VirtualEndpoint();
			ep.Configure(endpoint);

			var transport = new VirtualTransport(ep);

			return new Exchange(ep, transport);
		}

		#endregion
	}
}