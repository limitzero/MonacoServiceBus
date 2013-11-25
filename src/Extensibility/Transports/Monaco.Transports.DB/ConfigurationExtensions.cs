using System;
using Monaco.Transports.DB.Configuration;

namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static ITransportConfiguration UsingDB(this ITransportConfiguration configuration)
		{
			configuration.Register<Monaco.Transports.DB.SqlDbTransportRegistration>();
			TryExtractAndRegisterTransportSettings(configuration.Container);
			return configuration;
		}

		private static void TryExtractAndRegisterTransportSettings(IContainer container)
		{
			Exception exception = null;
			ISqlDbConfigurationSettings configuration = null;

			if (TryExtractSettingsFromLocalConfigurationFile(out configuration, out exception) == false)
			{
				string message = string.Concat("A problem has occurred while attempting to extract the settings for the sql db transport. ",
					"Please make sure to enter the correct configuration for the sql db transport in the local application configuration file.");
				throw new InvalidOperationException(message, exception);
			}

			if (TryRegisterSettingsInContainer(container, configuration, out exception) == false)
			{
				string message = string.Concat("A problem has occurred while register the settings for the sql db transport in the container.");
				throw new InvalidOperationException(message, exception);
			}
		}

		private static bool TryExtractSettingsFromLocalConfigurationFile(out ISqlDbConfigurationSettings configuration, out Exception exception)
		{
			bool success = false;
			exception = null;
			configuration = null;

			// extract the settings from the configuration file:
			try
			{
				configuration = Transports.DB.Configuration.ConfigurationSectionHandler.GetConfiguration();
				success = true;
			}
			catch (Exception retrieveFileTransportConfigurationException)
			{
				exception = retrieveFileTransportConfigurationException;
			}

			return success;
		}

		private static bool TryRegisterSettingsInContainer(IContainer container, ISqlDbConfigurationSettings configuration, out Exception exception)
		{
			bool success = false;
			exception = null;

			try
			{
				container.RegisterInstance<ISqlDbConfigurationSettings>(configuration);
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