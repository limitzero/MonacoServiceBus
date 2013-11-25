namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static ITransportConfiguration UsingMsmq(this ITransportConfiguration configuration)
		{
			configuration.Register<Monaco.Transports.Msmq.MsmqRegistration>();
			return configuration;
		}
	}
}