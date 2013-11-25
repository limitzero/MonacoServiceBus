namespace Monaco.Configuration.Endpoint
{
	public class EndpointConfiguration : IEndpointConfiguration
	{
		#region IEndpointConfiguration Members

		public string Name { get; set; }
		public string Uri { get; set; }
		public int Concurrency { get; set; }
		public string StatusInterval { get; set; }
		public string StatusIntervalGracePeriod { get; set; }
		public int MaxRetries { get; set; }

		#endregion
	}
}