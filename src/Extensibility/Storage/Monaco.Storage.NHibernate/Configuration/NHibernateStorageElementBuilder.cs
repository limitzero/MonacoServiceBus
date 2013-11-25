using System.Collections.Generic;
using Castle.Core;
using Castle.Core.Configuration;
using Castle.Facilities.FactorySupport;
using Castle.MicroKernel.Registration;
using Monaco.Configuration;
using Monaco.Configuration.Elements;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Storage.NHibernate.Sagas;
using Monaco.Storage.NHibernate.Subscriptions;
using Monaco.Storage.NHibernate.Timeouts;
using NHibernate;

namespace Monaco.Storage.NHibernate.Configuration
{
	public class NHibernateStorageElementBuilder : BaseElementBuilder
	{
		private const string elementName = "nhibernate.storage";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().Equals(elementName);
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			RegisterSagaStateMachineDataStorage();
			RegisterSubscriptionsStorage();
			RegisterTimeoutsStorage();

			var NHibernateConfiguration = new global::NHibernate.Cfg.Configuration();
			var properties = new Dictionary<string, string>();
			var mappingAssembly = string.Empty;

			RegisterConnectionSettings(configuration, out properties, out mappingAssembly);
			NHibernateConfiguration.Properties = properties;
			NHibernateConfiguration.AddAssembly(this.GetType().Assembly); // internal assembly for mappings

			if (string.IsNullOrEmpty(mappingAssembly) == false)
				NHibernateConfiguration.AddAssembly(mappingAssembly); // domain assembly for mappings.

			var sessionFactory = NHibernateConfiguration.BuildSessionFactory();

			Container.RegisterInstance<global::NHibernate.Cfg.Configuration>(NHibernateConfiguration);

			Container.Register<NHibernateConnectionFactory>();
			         
			Container.RegisterInstance<ISessionFactory>(sessionFactory);

			// use the factory method on the class to instantiate an on-demand session:
			Container.RegisterViaFactory<ISession>(()=> 
												   Container.Resolve<NHibernateConnectionFactory>().GetCurrentSession());

			// register the schema manager in case we need to create the data schema (testing only):
			Container.Register<INHibernateSchemaManager, NHibernateSchemaManager>();
		}

		private static void RegisterConnectionSettings(Castle.Core.Configuration.IConfiguration configuration,
			out Dictionary<string, string> properties,
			out string mappingAssemblyName)
		{
			properties = new Dictionary<string, string>();
			mappingAssemblyName = string.Empty;

			for (int index = 0; index < configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration nHibernateSetting = configuration.Children[index];

				var settingName = nHibernateSetting.Name;

				if (settingName.Trim().Equals("connection.provider"))
				{
					properties.Add("connection.provider", nHibernateSetting.Value);
				}

				if(settingName.Trim().Equals("connection.connection.string"))
				{
					properties.Add("connection.connection_string", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.provider.driver"))
				{
					properties.Add("connection.driver_class", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.show.sql"))
				{
					properties.Add("show.sql", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.dialect"))
				{
					properties.Add("dialect", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.use.outer.join"))
				{
					properties.Add("use_outer_join", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.command.timeout"))
				{
					properties.Add("command_timeout", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.query.substitutions"))
				{
					properties.Add("query.substitutions", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.proxy.factory"))
				{
					properties.Add("proxyfactory.factory_class", nHibernateSetting.Value);
				}

				if (settingName.Trim().Equals("connection.mapping.assembly"))
				{
					mappingAssemblyName = nHibernateSetting.Value;
				}
			}
		}

		private void RegisterSagaStateMachineDataStorage()
		{
			Container.Register(typeof(IStateMachineDataRepository<>),
					typeof(NHibernateStateMachineDataRepository<>));
		}

		private void RegisterTimeoutsStorage()
		{
			Container.Register<ITimeoutsRepository, NHibernateTimeoutsRepository>();
		}

		private void RegisterSubscriptionsStorage()
		{
			Container.Register<ISubscriptionRepository, NHibernateSubscriptionRepository>();
		}

	}
}