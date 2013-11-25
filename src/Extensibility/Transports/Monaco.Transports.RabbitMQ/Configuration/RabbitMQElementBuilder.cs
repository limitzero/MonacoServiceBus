using System;
using System.Collections.Generic;
using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Monaco.Configuration;
using Monaco.Configuration.Elements;

namespace Monaco.Transports.RabbitMQ.Configuration
{
	public class RabbitMQElementBuilder : BaseElementBuilder
	{
		private const string elementName = "rabbitmq.transport";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().Equals(elementName);
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			// extract off the information to get credentials to gain 
			// access to host server process (host, user name, password)
			// and store information in container:

			string exchange = string.Empty;
			string host = string.Empty;
			string username = string.Empty;
			string password = string.Empty;
			string protocol = string.Empty;
			int? port;
			
			RegisterConnectionSettings(configuration, out username, out password, out host, out exchange, out protocol, out  port);
			var rabbitMQConfiguration = new RabbitMQConfiguration(exchange, host, username, password, protocol, port);

			Container.RegisterInstance<RabbitMQConfiguration>(rabbitMQConfiguration);
		}

		private static void RegisterConnectionSettings(Castle.Core.Configuration.IConfiguration configuration,
			out string userName, 
			out string userPassword, 
			out string hostName,
			out string exchangeName, 
			out string protocol,
			out int? port)
		{
			userName = string.Empty;
			userPassword = string.Empty;
			exchangeName = string.Empty;
			hostName = string.Empty;
			protocol = string.Empty;
			port = null;

			for (int index = 0; index < configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration rabbitMQSetting = configuration.Children[index];

				var settingName = rabbitMQSetting.Name;

				if (settingName.Trim().Equals("user.name"))
				{
					userName = rabbitMQSetting.Value;
				}

				if (settingName.Trim().Equals("user.password"))
				{
					userPassword = rabbitMQSetting.Value;
				}

				if (settingName.Trim().Equals("host.name"))
				{
					hostName = rabbitMQSetting.Value;
				}

				if (settingName.Trim().Equals("exchange.name"))
				{
					exchangeName = rabbitMQSetting.Value;
				}

				if (settingName.Trim().Equals("port"))
				{
					int specifiedPort = 0;
					if(Int32.TryParse(rabbitMQSetting.Value, out specifiedPort) == true)
					{
						port = specifiedPort;
					}
				}

				if (settingName.Trim().Equals("protocol"))
				{
					protocol = rabbitMQSetting.Value;
				}
			}
		}
	}
}