using System;
using Monaco.Transport;

namespace Monaco.Endpoint.Impl.Bus
{
	public interface IBusEndpoint
	{
		ITransport Transport { get; set; }
		Uri Endpoint { get; set; }
		Uri ErrorEndpoint { get; set; }
		Uri LogEndpoint { get; set; }
		int Threads { get; set; }
		int Retries { get; set; }
	}
}