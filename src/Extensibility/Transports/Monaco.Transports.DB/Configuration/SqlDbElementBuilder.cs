using Castle.Core.Configuration;
using Castle.MicroKernel.Registration;
using Monaco.Configuration;
using Monaco.Configuration.Elements;

namespace Monaco.Transports.DB.Configuration
{
	public class SqlDbElementBuilder : BaseElementBuilder
	{
		private const string elementName = "sqldb.transport";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().Equals(elementName);
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			// extract off the information to get credentials to gain 
			// access to host server process (host, user name, password)
			// and store information in container:

			bool? autodelete = null;
			string server = string.Empty;
			string catalog = string.Empty;
			string username = string.Empty;
			string password = string.Empty;
			
			RegisterConnectionSettings(configuration, out username, out password, out server, out catalog, out autodelete);
			var sqlDbConfiguration = new SqlDbConfigurationSettings(username, password, server, catalog);
			sqlDbConfiguration.AutoDelete = autodelete;

			Container.RegisterInstance<SqlDbConfigurationSettings>(sqlDbConfiguration);
		}

		private static void RegisterConnectionSettings(Castle.Core.Configuration.IConfiguration configuration,
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
				Castle.Core.Configuration.IConfiguration sqlDBSetting = configuration.Children[index];

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
					if(flag == false)
					{
						autodelete = flag;
					}
				}
			}
		}
	}
}