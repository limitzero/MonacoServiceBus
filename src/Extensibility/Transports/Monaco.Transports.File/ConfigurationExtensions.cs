using System;
using Monaco.Transports.File.Configuration;

namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static ITransportConfiguration UsingFile(this ITransportConfiguration configuration)
		{
			configuration.Register<Monaco.Transports.File.FileTransportRegistration>();
			TryExtractAndRegisterFileTransportSettings(configuration.Container);
	
			return configuration;
		}

		private static void TryExtractAndRegisterFileTransportSettings(IContainer container)
		{
			Exception exception = null;
			IFileTransportConfiguration configuration = null;
			
			if(TryExtractSettingsFromLocalConfigurationFile(out configuration, out exception) == false)
			{
				string message = string.Concat("A problem has occurred while attempting to extract the settings for the file transport. " , 
					"Please make sure to enter the correct configuration for the file transport in the local application configuration file.",
					"Reason: {0}");
				throw new InvalidOperationException(message, exception);
			}

			if(TryRegisterSettingsInContainer(container, configuration, out exception) == false)
			{
				string message = string.Concat("A problem has occurred while register the settings for the file transport in the container. Reason: {0}");
				throw new InvalidOperationException(message, exception);
			}
		}

		private static bool TryExtractSettingsFromLocalConfigurationFile(out IFileTransportConfiguration configuration, out Exception exception)
		{
			bool success = false;
			exception = null;
			 configuration = null;

			// extract the settings from the configuration file:
			try
			{
				configuration = Transports.File.Configuration.ConfigurationSectionHandler.GetConfiguration();
				success = true;
			}
			catch (Exception retrieveFileTransportConfigurationException)
			{
				exception = retrieveFileTransportConfigurationException;
			}

			return success;
		}

		private static bool TryRegisterSettingsInContainer(IContainer container, IFileTransportConfiguration configuration, out Exception exception)
		{
			bool success = false;
			exception = null;

			try
			{
				container.RegisterInstance<IFileTransportConfiguration>(configuration);
				success = true;
			}
			catch (Exception couldNotRegisterFileTransportSettingsInContainerException)
			{
				exception = couldNotRegisterFileTransportSettingsInContainerException;
			}

			return success;
		}
	}
}