using System.Reflection;

namespace Monaco.Storage.NHibernate.Configuration
{
	public interface INHibernateConfiguration
	{
		/// <summary>
		/// This will read the NHibernate settings from the local app.config file in the current app domain.
		/// </summary>
		/// <returns></returns>
		INHibernateConfiguration WithApplicationConfiguration();

		/// <summary>
		/// This will read the custom NHibernate configuration from an external file in the run-time directory.
		/// </summary>
		/// <param name="configurationFile"></param>
		/// <returns></returns>
		INHibernateConfiguration WithConfigurationFile(string configurationFile);

		/// <summary>
		/// This will allow custom assemblies with mapped entities to be added to the configuration.
		/// </summary>
		/// <param name="assemblies"></param>
		/// <returns></returns>
		INHibernateConfiguration WithEntitiesFromAssembly(params Assembly[] assemblies);

		/// <summary>
		/// This will drop the existing schema and re-create it from the mappings.
		/// </summary>
		/// <returns></returns>
		INHibernateConfiguration DropAndCreateSchema();
	}
}