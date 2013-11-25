using System.Configuration;

namespace Monaco.Transports.File.Configuration
{
	/// <summary>
	/// Configuration section handler that will read all of the settings for the transport.
	/// </summary>
	public class ConfigurationSectionHandler :  ConfigurationSection, IFileTransportConfiguration
	{
		private const string SectionName = "file.transport";
		private const string ProcessedFileExtensionKey = "processed.file.extension";
		private const string SendFileExtensionKey = "send.file.extension";
		private const string ReceiveFileExtensionKey = "receive.file.extension";
		private const string MoveToDirectoryKey = "move.to.directory";
		private const string AutoDeleteKey = "auto.delete";

		public static IFileTransportConfiguration GetConfiguration()
		{
			return (ConfigurationSectionHandler)System.Configuration.ConfigurationManager.GetSection(SectionName);
		}

		[ConfigurationProperty(ProcessedFileExtensionKey, IsRequired = false, IsKey = false)]
		public string ProcessedFileExtension
		{
			get { return (string)this[ProcessedFileExtensionKey]; }
			set { this[ProcessedFileExtensionKey] = value; }
		}

		[ConfigurationProperty(SendFileExtensionKey, IsRequired = false, IsKey = false)]
		public string SendFileExtension
		{
			get { return (string)this[SendFileExtensionKey]; }
			set { this[SendFileExtensionKey] = value; }
		}

		[ConfigurationProperty(ReceiveFileExtensionKey, IsRequired = false, IsKey = false)]
		public string ReceiveFileExtension
		{
			get { return (string)this[ReceiveFileExtensionKey]; }
			set { this[ReceiveFileExtensionKey] = value; }
		}

		[ConfigurationProperty(MoveToDirectoryKey, IsRequired = false, IsKey = false)]
		public string MoveToDirectory
		{
			get { return (string)this[MoveToDirectoryKey]; }
			set { this[MoveToDirectoryKey] = value; }
		}

		[ConfigurationProperty(AutoDeleteKey, IsRequired = false, IsKey = false, DefaultValue = false)]
		public bool AutoDelete
		{
			get { return (bool)this[AutoDeleteKey]; }
			set { this[AutoDeleteKey] = value; }
		}
	}
}