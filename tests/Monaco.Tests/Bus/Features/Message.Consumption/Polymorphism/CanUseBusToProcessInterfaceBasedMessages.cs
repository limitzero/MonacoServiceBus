using System;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Message.Consumption.Polymorphism
{ 
	public class PolymorphicMessageTestEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			// sub-classes can not be found from component scanning the assembly, so manual map them:
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e =>
					e.MapMessages<CanUseBusToProcessInterfaceBasedMessages.InterfaceBasedMessageConsumer>()
					.MapMessages<CanUseBusToProcessInterfaceBasedMessages.LocalTradeMessageConsumer>()
					.MapMessages<CanUseBusToProcessInterfaceBasedMessages.MicrosoftMessageConsumer>());
		}
	}

	// the caveat here is that all properties should be nullable for interface-based messages (yuck...)
	public interface LocalTrade : IMessage
	{
		string Symbol { get; set; }
		decimal? Volume { get; set; }
	}

	public interface MicrosoftTrade : LocalTrade
	{
		decimal? IncurredPremium { get; set; }
	}

	public class CanUseBusToProcessInterfaceBasedMessages
	{
		public static ManualResetEvent Wait;
		public static MicrosoftTrade MicrosoftTrade;
		public static LocalTrade LocalTrade;
		private MonacoConfiguration configuration;

		public CanUseBusToProcessInterfaceBasedMessages()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<PolymorphicMessageTestEndpointConfiguration>(@"sample.config");
			Wait = new ManualResetEvent(false);
			MicrosoftTrade = null;
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
		public void can_create_message_from_interface_based_definition_and_consume_for_configured_endpoint()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				// ask the bus to create a message from the interface!!!
				var trade = bus.CreateMessage<MicrosoftTrade>();
				trade.Symbol = "MSFT";
				trade.Volume = 1200;

				bus.Start();

				bus.Publish(trade);

				Wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsAssignableFrom<MicrosoftTrade>(MicrosoftTrade);
			}
		}

		[Fact]
		public void can_use_bus_to_implement_polymorphic_message_dispatching()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				// ask the bus to create a message from the interface!!!
				var trade = bus.CreateMessage<MicrosoftTrade>();
				trade.Symbol = "MSFT";
				trade.Volume = 237.98M;

				bus.Start();

				// sending the message derived from the parent should 
				// also call the consumer that is handling the parent 
				// message as well as the consumer that is handling the 
				// child message (however one consumer component should not 
				// handle both the child and parent messages as a separation of concerns):
				// Ex: A IMicrosoftTrade IS A ILocalTrade
				bus.Publish(trade);

				Wait.WaitOne(TimeSpan.FromSeconds(10));
				Wait.Set();

				Assert.IsAssignableFrom<LocalTrade>(LocalTrade); // parent message
				Assert.IsAssignableFrom<MicrosoftTrade>(MicrosoftTrade); // child message
			}
		}

		public class MicrosoftMessageConsumer :
			Consumes<MicrosoftTrade>
		{
			public void Consume(MicrosoftTrade message)
			{
				MicrosoftTrade = message;
			}
		}

		public class LocalTradeMessageConsumer :
			Consumes<LocalTrade>
		{
			public void Consume(LocalTrade message)
			{
				LocalTrade = message;
			}

		}

		public class InterfaceBasedMessageConsumer :
			Consumes<MicrosoftTrade>
		{
			public void Consume(MicrosoftTrade message)
			{
				MicrosoftTrade = message;
				Wait.Set();
			}
		}
	}
}