using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Monaco.Extensibility.Transports;

namespace Monaco.Transports.DB.Configuration
{
	/// <summary>
	/// Transport boot-strapper for receiving and sending messages via SQL Server.
	/// </summary>
	public class SqlDbTransportBootstrapper : BaseTransportBootstrapper
	{
		private const string elementName = "sqldb.transport";

		public SqlDbTransportBootstrapper()
		{
			this.ElementName = elementName;
		}

		public override void ExtractElementSectionToConfigureTransport(Castle.Core.Configuration.IConfiguration configuration)
		{
			bool? autodelete = null;
			string server = string.Empty;
			string catalog = string.Empty;
			string username = string.Empty;
			string password = string.Empty;

			RetreiveSettings(configuration, out username, out password, out server, out catalog, out autodelete);
			var sqlDbConfiguration = new SqlDbConfigurationSettings(username, password, server, catalog);
			sqlDbConfiguration.AutoDelete = autodelete;
			Container.RegisterInstance<SqlDbConfigurationSettings>(sqlDbConfiguration);
		}

		private static void RetreiveSettings(IConfiguration configuration,
				out string userName,
				out string userPassword,
				out string server,
				out string catalog,
				out bool? autodelete)
		{
			userName = string.Empty;
			userPassword = string.Empty;
			server = string.Empty;
			catalog = string.Empty;
			autodelete = true;

			for (int index = 0; index < configuration.Children.Count; index++)
			{
				IConfiguration sqlDBSetting = configuration.Children[index];

				var settingName = sqlDBSetting.Name;

				if (settingName.Trim().Equals("user.name"))
				{
					userName = sqlDBSetting.Value;
				}

				if (settingName.Trim().Equals("user.password"))
				{
					userPassword = sqlDBSetting.Value;
				}

				if (settingName.Trim().Equals("server.name"))
				{
					server = sqlDBSetting.Value;
				}

				if (settingName.Trim().Equals("catalog.name"))
				{
					catalog = sqlDBSetting.Value;
				}

				if (settingName.Trim().Equals("auto.delete"))
				{
					bool flag = false; // counter of assumption above to always remove messages:
					bool.TryParse(sqlDBSetting.Value, out flag);

					// change it if found...
					if (flag == false)
					{
						autodelete = flag;
					}
				}
			}
		}
	}
}