using System;
using Monaco.Transport;

namespace Monaco.Endpoint.Impl.Bus
{
	public class BusEndpoint : IBusEndpoint
	{
		#region IBusEndpoint Members

		public ITransport Transport { get; set; }
		public Uri Endpoint { get; set; }
		public Uri ErrorEndpoint { get; set; }
		public Uri LogEndpoint { get; set; }
		public int Threads { get; set; }
		public int Retries { get; set; }

		#endregion
	}
}