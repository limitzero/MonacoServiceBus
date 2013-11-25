using System;

namespace Monaco.Endpoint.Impl.Bus
{
	public interface IStandaloneEndpoint
	{
		Uri Endpoint { get; set; }
	}
}