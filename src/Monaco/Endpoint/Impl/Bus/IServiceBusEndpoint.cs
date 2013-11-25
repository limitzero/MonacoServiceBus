namespace Monaco.Endpoint.Impl.Bus
{
	public interface IServiceBusEndpoint : IStandaloneEndpoint
	{
		int Retries { get; set; }
		int Threads { get; set; }
	}
}