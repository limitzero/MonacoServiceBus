using System;

namespace Monaco.Endpoint.Impl.Bus
{
	public class ServiceBusErrorEndpoint : IServiceBusErrorEndpoint
	{
		#region IServiceBusErrorEndpoint Members

		public Uri Endpoint { get; set; }

		#endregion
	}
}