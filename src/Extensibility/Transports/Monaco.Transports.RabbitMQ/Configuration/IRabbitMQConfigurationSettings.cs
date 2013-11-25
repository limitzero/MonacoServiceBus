namespace Monaco.Transports.RabbitMQ.Configuration
{
	public interface IRabbitMQConfigurationSettings
	{
		string Exchange { get; }
		string Host { get; }
		string UserName { get; }
		string Password { get; }
		string Protocol { get; }
		int Port { get; }
	}
}