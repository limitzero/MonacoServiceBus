using System.Configuration;

namespace Monaco.Transports.DB.Configuration
{
	/// <summary>
	/// Configuration section handler that will read all of the settings for the transport.
	/// </summary>
	public class ConfigurationSectionHandler :  ConfigurationSection, ISqlDbConfigurationSettings
	{
		private const string SectionName = "sqldb.transport";
		private const string UserNameKey = "user.name";
		private const string PasswordKey = "user.password";
		private const string ServerKey = "server.name";
		private const string CatalogKey = "catalog.name";
		private const string AutoDeleteKey = "auto.delete";

		public static ISqlDbConfigurationSettings GetConfiguration()
		{
			return (ConfigurationSectionHandler)System.Configuration.ConfigurationManager.GetSection(SectionName);
		}

		[ConfigurationProperty(UserNameKey, IsRequired = false, IsKey = false)]
		public string UserName 
		{
			get { return (string)this[UserNameKey]; }
			set { this[UserNameKey] = value; } 
		}

		[ConfigurationProperty(PasswordKey, IsRequired = false, IsKey = false)]
		public string Password
		{
			get { return (string)this[PasswordKey]; }
			set { this[PasswordKey] = value; } 
		}

		[ConfigurationProperty(ServerKey, IsRequired = true)]
		public string Server
		{
			get { return (string)this[ServerKey]; }
			set { this[ServerKey] = value; }
		}

		[ConfigurationProperty(CatalogKey, IsRequired = true)]
		public string Catalog
		{
			get { return (string)this[CatalogKey]; }
			set { this[CatalogKey] = value; }
		}

		[ConfigurationProperty(AutoDeleteKey, IsRequired = false, IsKey = false, DefaultValue = false)]
		public bool AutoDelete
		{
			get { return (bool)this[AutoDeleteKey]; }
			set { this[AutoDeleteKey] = value; }
		}
	}
}