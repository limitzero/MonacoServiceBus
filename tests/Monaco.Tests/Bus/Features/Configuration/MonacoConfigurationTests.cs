using System;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Configuration
{

	public class ConfigurationTestsEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(s => s.SupportsTransactions(true));
		}
	}

	public class MonacoConfigurationTests : IDisposable
	{
		private MonacoConfiguration configuration;
		private ManualResetEvent wait;

		public MonacoConfigurationTests()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<ConfigurationTestsEndpointConfiguration>(@"sample.config");
			wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			configuration.Dispose();
			configuration = null;

			if (wait != null)
			{
				wait.Close();
				wait = null;
			}
		}

		[Fact]
		public void can_resolve_service_bus_instance_from_configuration_container()
		{
			var bus = configuration.Container.Resolve<IServiceBus>();
			Assert.NotNull(bus);
		}

		[Fact]
		public void can_start_and_stop_instance_of_service_bus_retreived_from_configuration_container()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();
				Assert.True(bus.IsRunning);

				wait.WaitOne(TimeSpan.FromSeconds(5));
				wait.Set();

				bus.Stop();
				Assert.False(bus.IsRunning);
			}
		}

	}
}