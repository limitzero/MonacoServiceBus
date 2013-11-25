using System;
using System.Collections.Generic;
using System.Reflection;
using Monaco.Configuration;
using Monaco.Storage.NHibernate.Sagas;
using Monaco.Storage.NHibernate.Subscriptions;
using Monaco.Storage.NHibernate.Timeouts;
using NHibernate;

namespace Monaco.Storage.NHibernate.Configuration.Impl
{
	internal class NHibernateConfiguration : INHibernateConfiguration
	{
		private readonly IContainer container;
		private string configurationFile = string.Empty;
		private IEnumerable<Assembly> assemblies;
		private bool dropAndCreateOnConfigure;

		public NHibernateConfiguration(IContainer container)
		{
			this.container = container;
		}

		public INHibernateConfiguration WithApplicationConfiguration()
		{
			this.configurationFile = string.Empty;
			return this;
		}

		public INHibernateConfiguration WithConfigurationFile(string configurationFile)
		{
			if (string.IsNullOrEmpty(configurationFile))
				throw new InvalidOperationException(
					"The configuration file to setup NHibernate persistance must be suppled for the option 'UsingConfigurationFile(...)");
			this.configurationFile = configurationFile;
			return this;
		}

		public INHibernateConfiguration WithEntitiesFromAssembly(params Assembly[] assemblies)
		{
			this.assemblies = assemblies;
			return this;
		}

		public INHibernateConfiguration DropAndCreateSchema()
		{
			this.dropAndCreateOnConfigure = true;
			return this;
		}

		public void Configure(IStorageConfiguration storageConfiguration)
		{
			// configure the NHibernate environment:
			Exception exception = null;
			if (TryConfigureNHibernate(storageConfiguration.Configuration.Container, out exception) == false)
			{
				throw new InvalidOperationException("An error has ocurred while attempting to configure NHibernate", exception);
			}

			// create the storage components:
			storageConfiguration.TimeoutsRepository = typeof(NHibernateTimeoutsRepository);
			storageConfiguration.SubscriptionRepository = typeof(NHibernateSubscriptionRepository);
			storageConfiguration.StateMachineDataRepository = typeof(NHibernateStateMachineDataRepository<>);
		}

		private bool TryConfigureNHibernate(IContainer container, out Exception exception)
		{
			bool success = false;
			exception = null;
			global::NHibernate.Cfg.Configuration configuration = new global::NHibernate.Cfg.Configuration();

			Exception loadFromLocalConfigurationException = null;
			Exception loadFromExternalConfigurationException = null;

			if (TryConfigureFromLocalConfigurationFile(ref configuration, out loadFromLocalConfigurationException) == false)
			{
				exception = loadFromLocalConfigurationException;

				if (TryConfigureFromExternalConfigurationFile(ref configuration, out loadFromExternalConfigurationException) == false)
				{
					exception = loadFromExternalConfigurationException;
				}
			}

			if (exception != null)
			{
				string message = "An error has occurred while attempting to configure NHibernate. Reason: {0}";
				throw new InvalidOperationException(string.Format(message, exception));
			}

			// build up the configuration and the session factory:
			try
			{
				this.AddAllReferencedAssemblies(configuration);

				var sessionFactory = configuration.BuildSessionFactory();
				container.RegisterInstance<global::NHibernate.Cfg.Configuration>(configuration);
				container.Register<NHibernateConnectionFactory>();
				container.RegisterInstance<ISessionFactory>(sessionFactory);

				// use the factory method on the class to instantiate an on-demand session:
				container.RegisterViaFactory<ISession>(() =>
													   container.Resolve<NHibernateConnectionFactory>().GetCurrentSession());

				// register the schema manager in case we need to create the data schema (testing only):
				container.Register<INHibernateSchemaManager, NHibernateSchemaManager>();

				if (TryDropAndCreateSchema(ref configuration, out exception) == false)
				{
					throw exception;
				}

				success = true;
			}
			catch (Exception couldNotBuildSessionFactory)
			{
				string message =
					"An error has occurred while attempting to build the session factory from the NHibernate configuration. Reason: {0}";
				exception = new InvalidOperationException(string.Format(message, couldNotBuildSessionFactory.Message),
														  couldNotBuildSessionFactory);
			}

			return success;
		}
		
		private bool TryDropAndCreateSchema(ref global::NHibernate.Cfg.Configuration configuration,
															   out  Exception exception)
		{
			bool success = false;
			exception = null;

			// re-create the schema if desired:
			if (this.dropAndCreateOnConfigure == true)
			{
				try
				{
					var schemaManager = container.Resolve<INHibernateSchemaManager>();
					schemaManager.DropSchema();
					schemaManager.CreateSchema();
					success = true;
				}
				catch (Exception dropAndCreateSchemaException)
				{
					string message =
						"An error has occurred while attempting to drop and re-create the schema based on the NHibernate mappings. Reason: {0}";
					exception = new InvalidOperationException(string.Format(message, dropAndCreateSchemaException.Message),
					                          dropAndCreateSchemaException);
				}
			}

			return success;
		}

		private bool TryConfigureFromExternalConfigurationFile(ref global::NHibernate.Cfg.Configuration configuration,
															   out  Exception exception)
		{
			bool success = false;
			exception = null;

			if (string.IsNullOrEmpty(this.configurationFile) == false)
			{
				try
				{
					configuration.Configure(this.configurationFile);
					success = true;
				}
				catch (Exception couldNotConfigureFromExternalConfigurationFileException)
				{
					string message = "Could not configure NHibernate from external application configuration file '{0}'. Reason: {1}";
					exception = new Exception(string.Format(message,
															this.configurationFile,
															couldNotConfigureFromExternalConfigurationFileException.Message),
											  couldNotConfigureFromExternalConfigurationFileException);
				}
			}

			return success;
		}

		private bool TryConfigureFromLocalConfigurationFile(ref global::NHibernate.Cfg.Configuration configuration,
															out Exception exception)
		{
			bool success = false;
			exception = null;

			try
			{
				configuration.Configure();
				success = true;
			}
			catch (Exception couldNotConfigureFromLocalConfigurationFileException)
			{
				string message = "Could not configure NHibernate from local application configuration file. Reason: {0}";
				exception = new Exception(string.Format(message,
														couldNotConfigureFromLocalConfigurationFileException.Message),
										  couldNotConfigureFromLocalConfigurationFileException);
			}

			return success;
		}

		private void AddAllReferencedAssemblies(global::NHibernate.Cfg.Configuration configuration)
		{
			if (this.assemblies == null) return;

			foreach (var assembly in assemblies)
			{
				configuration.AddAssembly(assembly);
			}
		}
	}
}