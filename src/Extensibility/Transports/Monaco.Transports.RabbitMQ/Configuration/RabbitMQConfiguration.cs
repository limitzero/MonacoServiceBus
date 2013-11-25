namespace Monaco.Transports.RabbitMQ.Configuration
{
	public class RabbitMQConfiguration 
	{
		public string Exchange { get; private set; }
		public string Host { get; private set; }
		public string UserName { get; private set; }
		public string Password { get; private set; }
		public string Protocol { get; private set; }
		public int? Port { get; private set; }

		public RabbitMQConfiguration(string exchange, string host, string userName, string password, string protocol, int? port)
		{
			Exchange = exchange;
			Host = host;
			UserName = userName;
			Password = password;
			this.Protocol = protocol;
			this.Port = port;
		}
	}
}