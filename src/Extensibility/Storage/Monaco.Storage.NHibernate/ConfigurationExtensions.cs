using System;
using System.Linq.Expressions;
using Monaco.Configuration;
using Monaco.Storage.NHibernate.Configuration;
using Monaco.Storage.NHibernate.Configuration.Impl;
using Monaco.Storage.NHibernate.Sagas;
using Monaco.Storage.NHibernate.Subscriptions;
using Monaco.Storage.NHibernate.Timeouts;

namespace Monaco.Storage.NHibernate
{
	public static class ConfigurationExtensions
	{
		public static IStorageConfiguration UsingNHibernate(this IStorageConfiguration storageConfiguration,
			Expression<Func<INHibernateConfiguration, INHibernateConfiguration>> nhibernateCofinguration)
		{
			storageConfiguration.TimeoutsRepository = typeof(NHibernateTimeoutsRepository);
			storageConfiguration.SubscriptionRepository = typeof(NHibernateSubscriptionRepository);
			storageConfiguration.StateMachineDataRepository = typeof(NHibernateStateMachineDataRepository<>);

			((Monaco.Configuration.Configuration)storageConfiguration.Configuration)
				.BindExtensibilityAction(() => BindExtensibility(storageConfiguration, nhibernateCofinguration));

			return storageConfiguration;
		}

		private static void BindExtensibility(IStorageConfiguration storageConfiguration, 
				Expression<Func<INHibernateConfiguration, INHibernateConfiguration>> nhibernateCofinguration)
			{
				INHibernateConfiguration configuration =
					nhibernateCofinguration.Compile().Invoke(new NHibernateConfiguration(storageConfiguration.Configuration.Container));
				((NHibernateConfiguration)configuration).Configure(storageConfiguration);
			}
	}
}