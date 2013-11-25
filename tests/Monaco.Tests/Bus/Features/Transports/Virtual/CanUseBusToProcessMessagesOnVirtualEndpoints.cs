using System;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Transports.Virtual
{
	public class VirutalEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingInMemory()) // this is the virtual endpoint
				.WithEndpoint(s => s.MapAll(this.GetType().Assembly));
		}
	}

	public class CanUseBusToProcessMessagesOnVirtualEndpoints
	{
		public static int ReceivedCount;
		public static ManualResetEvent Wait;
		public static IMessage ReceivedMessage;
		private MonacoConfiguration configuration;

		public CanUseBusToProcessMessagesOnVirtualEndpoints()
		{
			// using multiple threads to consume messages from endpoint:
			configuration = MonacoConfiguration
				.BootFromEndpoint<VirutalEndpointConfiguration>(@"sample.virtual.config");
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
		public void can_use_bus_to_publish_a_message_to_a_consumer_on_virtual_endpoint()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<VirtualEndpointMessageConsumer>();
				bus.Start();

				bus.Publish(new VirtualMessage());
				Wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsType<VirtualMessage>(ReceivedMessage);
			}

		}

		public class VirtualMessage : IMessage
		{
		}

		public class VirtualEndpointMessageConsumer
			: TransientConsumerOf<VirtualMessage>
		{
			public void Consume(VirtualMessage message)
			{
				ReceivedMessage = message;
				ReceivedCount++;
				System.Diagnostics.Debug.WriteLine("Message received: " + ReceivedCount.ToString());
				Wait.Set();
			}
		}

	}


}