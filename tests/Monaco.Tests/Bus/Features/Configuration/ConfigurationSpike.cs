using System;
using Monaco.Configuration;
using Monaco.Containers.Windsor;
using Monaco.Endpoint.Factory;
using Monaco.Extensibility.Storage.Impl.Volatile;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Transport.Virtual;
using Xunit;

namespace Monaco.Tests.Bus.Features.Configuration
{
	public class MyEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingNullContainer())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingInMemory())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly));
		}
	}

	public class ConfigurationSystemTests
	{
		[Fact]
		public void can_load_default_storage_and_transport_options_from_configuration_system()
		{
			IConfiguration configuration = Monaco.Configuration.Configuration.Create();

			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingInMemory())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly));

			((Monaco.Configuration.Configuration)configuration).Configure();

			var exchange = configuration.Container.Resolve<IEndpointFactory>().Build(new Uri("vm://test"));
		
			Assert.Equal(typeof(WindsorContainerAdapter), configuration.Container.GetType());
			Assert.Equal(typeof(VirtualTransport), exchange.Transport.GetType());
			Assert.Equal(typeof(InMemoryTimeoutsRepository), configuration.Container.Resolve<ITimeoutsRepository>().GetType());
			Assert.Equal(typeof(InMemorySubscriptionRepository), configuration.Container.Resolve<ISubscriptionRepository>().GetType());
		}
	}

}
