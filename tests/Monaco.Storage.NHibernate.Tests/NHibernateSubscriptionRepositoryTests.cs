using System;
using System.Collections.Generic;
using Monaco.Configuration;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;
using Xunit;

namespace Monaco.Storage.NHibernate.Tests
{
	public class NHibernateSubscriptionRepositoryEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingNHibernate(h => h.WithConfigurationFile(@"hibernate.cfg.xml")
					 .WithEntitiesFromAssembly(this.GetType().Assembly)
					 .DropAndCreateSchema()))
				.WithTransport(t => t.UsingInMemory())
				.WithEndpoint(e => e.SupportsTransactions(false));
		}
	}

	public class NHibernateSubscriptionRepositoryTests : IDisposable
	{
		private MonacoConfiguration configuration;
		private readonly ISubscription subscription;

		public NHibernateSubscriptionRepositoryTests()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<NHibernateSubscriptionRepositoryEndpointConfiguration>(@"sample.config");
			subscription = DomainUtils.CreateSubscription();
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;
		}

		[Fact]
		public void can_use_repository_to_register_a_subscription()
		{
			var repository = configuration.Container.Resolve<ISubscriptionRepository>();
			repository.Register(subscription);

			ICollection<Subscription> subscriptions = repository.Find(new MyMessage().GetType());

			Assert.Equal(1, subscriptions.Count);
			Assert.Equal(new List<Subscription>(subscriptions)[0].Message, typeof(MyMessage).FullName);
		}

		[Fact]
		public void can_use_repository_to_unregister_a_subscription()
		{
			var repository = configuration.Container.Resolve<ISubscriptionRepository>();
			repository.Register(subscription);

			ICollection<Subscription> subscriptions = repository.Find(new MyMessage().GetType());

			Assert.Equal(1, subscriptions.Count);
			Assert.Equal(new List<Subscription>(subscriptions)[0].Message, typeof(MyMessage).FullName);

			repository.Unregister(subscription);
			subscriptions = repository.Find(new MyMessage().GetType());

			Assert.Equal(0, subscriptions.Count);
		}

		[Fact]
		public void can_use_repository_to_return_all_subscriptions()
		{
			var repository = configuration.Container.Resolve<ISubscriptionRepository>();
			repository.Register(subscription);

			ICollection<Subscription> subscriptions = repository.Subscriptions;

			Assert.Equal(1, subscriptions.Count);
			Assert.Equal(new List<Subscription>(subscriptions)[0].Message,
					typeof(MyMessage).FullName);
		}

	}
}