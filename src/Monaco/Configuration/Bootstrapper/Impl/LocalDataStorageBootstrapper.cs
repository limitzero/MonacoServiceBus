using Castle.Core;
using Monaco.Configuration.Bootstrapper.Roles;
using Monaco.Extensibility.Storage.Impl.Volatile;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;

namespace Monaco.Configuration.Bootstrapper.Impl
{
	public class LocalDataStorageBootstrapper : BaseBusStorageProviderBootstrapper
	{
		public LocalDataStorageBootstrapper()
		{
			this.IsActive = false;
		}

		public override void Configure()
		{
			try
			{
				var timeoutsRepository = Container.Resolve<ITimeoutsRepository>();
			}
			catch
			{
				// use the in-memory implementation:
				Container.Register<ITimeoutsRepository, InMemoryTimeoutsRepository>(ContainerLifeCycle.Singleton);
			}

			try
			{
				var subscriptionRepository = Container.Resolve<ISubscriptionRepository>();
			}
			catch
			{
				// use the in-memory implementation:
				Container.Register<ISubscriptionRepository, InMemorySubscriptionRepository>(ContainerLifeCycle.Singleton);
			}
		}
	}
}