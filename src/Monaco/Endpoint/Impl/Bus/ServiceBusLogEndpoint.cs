using System;

namespace Monaco.Endpoint.Impl.Bus
{
	public class ServiceBusLogEndpoint : IServiceBusLogEndpoint
	{
		#region IServiceBusLogEndpoint Members

		public Uri Endpoint { get; set; }

		#endregion
	}
}