using System.Configuration;

namespace Monaco.Transports.RabbitMQ.Configuration
{
	/// <summary>
	/// Configuration section handler that will read all of the settings for the transport.
	/// </summary>
	public class ConfigurationSectionHandler :  ConfigurationSection, IRabbitMQConfigurationSettings
	{
		private const string SectionName = "rabbitmq";
		private const string UserNameKey = "username";
		private const string PasswordKey = "password";
		private const string HostNameKey = "host";
		private const string ExchangeNameKey = "exchange";
		private const string PortKey = "port";
		private const string ProtocolKey = "protocol";

		public static IRabbitMQConfigurationSettings GetConfiguration()
		{
			return (ConfigurationSectionHandler)System.Configuration.ConfigurationManager.GetSection(SectionName);
		}

		[ConfigurationProperty(UserNameKey, IsRequired = true, IsKey = false)]
		public string UserName 
		{
			get { return (string)this[UserNameKey]; }
			set { this[UserNameKey] = value; } 
		}

		[ConfigurationProperty(PasswordKey, IsRequired = true, IsKey = false)]
		public string Password
		{
			get { return (string)this[PasswordKey]; }
			set { this[PasswordKey] = value; } 
		}

		[ConfigurationProperty(ExchangeNameKey, IsRequired =  true)]
		public string Exchange
		{
			get { return (string)this[ExchangeNameKey]; }
			set { this[ExchangeNameKey] = value; } 
		}

		[ConfigurationProperty(HostNameKey, IsRequired = true)]
		public string Host
		{
			get { return (string)this[HostNameKey]; }
			set { this[HostNameKey] = value; }
		}

		[ConfigurationProperty(ProtocolKey, IsRequired = true, DefaultValue = "AMQP_0_9_1")]
		public string Protocol
		{
			get { return (string)this[ProtocolKey]; }
			set { this[ProtocolKey] = value; }
		}

		[ConfigurationProperty(PortKey, IsRequired = false, IsKey = false)]
		public int Port
		{
			get { return (int)this[PortKey]; }
			set { this[PortKey] = value; }
		}
	}
}