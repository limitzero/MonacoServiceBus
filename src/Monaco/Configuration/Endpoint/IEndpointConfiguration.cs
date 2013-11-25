namespace Monaco.Configuration.Endpoint
{
	public interface IEndpointConfiguration
	{
		string Name { get; set; }
		string Uri { get; set; }
		int Concurrency { get; set; }
		string StatusInterval { get; set; }
		string StatusIntervalGracePeriod { get; set; }
		int MaxRetries { get; set; }
	}
}