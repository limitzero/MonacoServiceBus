using System;
using System.Collections.Generic;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Subscriptions
{
	public class BusInstanceSubscriptionsEndpoint : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly));
		}
	}

	public class BusInstanceSubscriptionTests : IDisposable
	{
		public static ManualResetEvent wait;
		public static IMessage receivedMessage;
		public static List<IMessage> receivedMessages;
		private MonacoConfiguration configuration;

		public BusInstanceSubscriptionTests()
		{
			configuration =
				MonacoConfiguration.BootFromEndpoint<BusInstanceSubscriptionsEndpoint>(@"sample.config");
			wait = new ManualResetEvent(false);
			receivedMessages = new List<IMessage>();
		}

		public void Dispose()
		{
			receivedMessages.Clear();
			receivedMessages = null;

			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;

			if (wait != null)
			{
				wait.Close();

			}
			wait = null;
		}

		[Fact]
		public void can_register_instance_subscription_on_bus_for_message_consumption_and_send_message()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			using (bus.AddInstanceConsumer<TransientMessageConsumer>())
			{
				bus.Start();

				bus.Send(new TransientMessage());
				wait.WaitOne(TimeSpan.FromSeconds(5));
				wait.Reset();

				// "Sent" messages are proxied for replies to the receipient across boundaries:
				Assert.IsAssignableFrom<TransientMessage>(receivedMessage);
			}
		}

		[Fact]
		public void can_register_instance_subscription_on_bus_for_message_consumption_and_publish_message()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			using (bus.AddInstanceConsumer<TransientMessageConsumer>())
			{
				bus.Start();

				bus.Publish(new TransientMessage());

				wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsAssignableFrom<TransientMessage>(receivedMessage);
			}
		}

		[Fact]
		public void can_generate_exception_when_instance_subscription_does_not_implement_transient_consumer_interface()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				Assert.Throws<Exception>(
										() => bus.AddInstanceConsumer<TransientMessageConsumerWithWrongInterfacesForConsumption>());
			}
		}

		[Fact]
		public void can_generate_exception_when_instance_subscription_has_messages_defined_as_interfaces()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				Assert.Throws<Exception>(
										 () => bus.AddInstanceConsumer<TransientMessageConsumerWithInterface>());
			}
		}

		public interface ITransientMessage : IMessage
		{ }

		public class TransientMessage : IMessage
		{ }

		public class TransientMessageConsumer :
			TransientConsumerOf<TransientMessage>
		{
			public void Consume(TransientMessage message)
			{
				receivedMessage = message;
				wait.Set();
			}
		}

		public class TransientMessageConsumerWithInterface :
				TransientConsumerOf<ITransientMessage>
		{
			public void Consume(ITransientMessage message)
			{
				receivedMessages.Add(message);
				wait.Set();
			}
		}

		public class TransientMessageConsumerWithWrongInterfacesForConsumption
			: Consumes<ITransientMessage>,
			  Consumes<TransientMessage>
		{
			public void Consume(ITransientMessage message)
			{
			}

			public void Consume(TransientMessage message)
			{
			}
		}

	}
}