using System;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Modules.Bus
{
	// endpoint to tell bus that a module is present for hosting:
	public class BusModuleEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.ConfigureBusModules(this.GetType().Assembly));
		}
	}


	public class CanUseBusToHostModuleOverBusLifecycle : IDisposable
	{
		public static ManualResetEvent Wait;
		public static bool ModuleStarted;
		public static bool ModuleStopped;
		private MonacoConfiguration configuration;

		public CanUseBusToHostModuleOverBusLifecycle()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint < BusModuleEndpointConfig>(@"sample.config");
			Wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;

			if (Wait != null)
			{
				Wait.Close();
			}
			Wait = null;
		}

		[Fact]
		public void can_use_bus_to_host_bus_module_for_duration_of_bus_lifecycle()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				Wait.WaitOne(TimeSpan.FromSeconds(5));
				Assert.True(ModuleStarted, 
					"The bus module could not be started when the bus was started.");
			}
			Assert.True(ModuleStopped,
				"The bus module could not be stopped when the bus was stopped.");
		}

		// module that is hosted on the bus (active as long as the bus is active!!!):
		public class SampleBusModule : IBusModule
		{
			public void Dispose()
			{
				ModuleStopped = true;
			}

			public void Start(IContainer container)
			{
				ModuleStarted = true;
				if (Wait != null)
					Wait.Set();
			}
		}
	}
}