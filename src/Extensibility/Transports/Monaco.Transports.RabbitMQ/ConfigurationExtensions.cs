using System;
using Monaco.Transports.RabbitMQ.Configuration;

namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static ITransportConfiguration UsingRabbitMQ(this ITransportConfiguration configuration)
		{
			configuration.Register<Monaco.Transports.RabbitMQ.RabbitMqTransportRegistration>();
			TryExtractAndRegisterTransportSettings(configuration.Container);
			return configuration;
		}

		private static void TryExtractAndRegisterTransportSettings(IContainer container)
		{
			Exception exception = null;
			IRabbitMQConfigurationSettings configuration = null;

			if (TryExtractSettingsFromLocalConfigurationFile(out configuration, out exception) == false)
			{
				string message = string.Concat("A problem has occurred while attempting to extract the settings for the RabbitMQ transport. ",
					"Please make sure to enter the correct configuration for the RabbitMQ transport in the local application configuration file.");
				throw new InvalidOperationException(message, exception);
			}

			if (TryRegisterSettingsInContainer(container, configuration, out exception) == false)
			{
				string message = string.Concat("A problem has occurred while register the settings for the RabbitMQ transport in the container.");
				throw new InvalidOperationException(message, exception);
			}
		}

		private static bool TryExtractSettingsFromLocalConfigurationFile(out IRabbitMQConfigurationSettings configuration, out Exception exception)
		{
			bool success = false;
			exception = null;
			configuration = null;

			// extract the settings from the configuration file:
			try
			{
				configuration = Transports.RabbitMQ.Configuration.ConfigurationSectionHandler.GetConfiguration();
				success = true;
			}
			catch (Exception retrieveFileTransportConfigurationException)
			{
				exception = retrieveFileTransportConfigurationException;
			}

			return success;
		}

		private static bool TryRegisterSettingsInContainer(IContainer container, IRabbitMQConfigurationSettings configuration, out Exception exception)
		{
			bool success = false;
			exception = null;

			try
			{
				container.RegisterInstance<IRabbitMQConfigurationSettings>(configuration);
				success = true;
			}
			catch (Exception couldNotRegisterTransportSettingsInContainerException)
			{
				exception = couldNotRegisterTransportSettingsInContainerException;
			}

			return success;
		}
	}
}