using System;

namespace Monaco.Endpoint.Impl.Bus
{
	public class ServiceBusEndpoint : IServiceBusEndpoint
	{
		#region IServiceBusEndpoint Members

		public Uri Endpoint { get; set; }
		public int Retries { get; set; }
		public int Threads { get; set; }

		#endregion
	}
}