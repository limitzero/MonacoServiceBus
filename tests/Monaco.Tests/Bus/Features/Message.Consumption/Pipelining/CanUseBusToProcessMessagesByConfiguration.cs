using System;
using System.Collections.Generic;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Message.Consumption.Pipelining
{
	public class EndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e =>
					e.	MapMessages<CanUseBusToProcessMessagesByConfiguration.BusEndpointConfiguredHandlers1>()
						.MapMessages<CanUseBusToProcessMessagesByConfiguration.BusEndpointConfiguredHandlers2>()
						// here is the configuration on how to handle a message (similar to a pipeline):
						.ConfigureMessageConsumerHandlingChain(c =>
							c.ForMessage<CanUseBusToProcessMessagesByConfiguration.BusEndpointConfiguredHandlersMessage>()
							  .InitiallyHandledBy<CanUseBusToProcessMessagesByConfiguration.BusEndpointConfiguredHandlers1>()
							  .FollowedBy<CanUseBusToProcessMessagesByConfiguration.BusEndpointConfiguredHandlers2>()
						));
		}
	}

	public class CanUseBusToProcessMessagesByConfiguration : IDisposable
	{
		public static List<object> ConfiguredHandlers;
		public static ManualResetEvent Wait;
		private MonacoConfiguration configuration;

		public CanUseBusToProcessMessagesByConfiguration()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint < EndpointConfiguration>(@"sample.config");

			Wait = new ManualResetEvent(false);
			ConfiguredHandlers = new List<object>();
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
		public void can_configure_a_predefined_order_for_handling_message_from_endpoint_configuration()
		{
			using(var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();
				bus.Publish(new BusEndpointConfiguredHandlersMessage());

				Wait.WaitOne(TimeSpan.FromSeconds(5));
			
				Assert.IsType<BusEndpointConfiguredHandlers1>(ConfiguredHandlers[0]);
				Assert.IsType<BusEndpointConfiguredHandlers2>(ConfiguredHandlers[1]);
			}
		}

		public class BusEndpointConfiguredHandlersMessage : IMessage
		{ }

		public class BusEndpointConfiguredHandlers1
			: Consumes<BusEndpointConfiguredHandlersMessage>
		{
			public void Consume(BusEndpointConfiguredHandlersMessage message)
			{
				ConfiguredHandlers.Add(this);
			}
		}

		public class BusEndpointConfiguredHandlers2
			: Consumes<BusEndpointConfiguredHandlersMessage>
		{
			public void Consume(BusEndpointConfiguredHandlersMessage message)
			{
				ConfiguredHandlers.Add(this);
				Wait.Set();
			}
		}


	}
}