using System;
using System.Threading;
using Monaco.Bus;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Xunit;

namespace Monaco.Transports.Msmq.Tests
{
	public class MsmqTransportEndpoint : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(t => t.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly)
				                   	.SupportsTransactions(true));
		}
	}

	public class MsmqTransportTests : IDisposable
	{
		public static int received_count;
		public static ManualResetEvent wait;
		public static IMessage received_message;
		private MonacoConfiguration configuration;

		public MsmqTransportTests()
		{
			this.configuration =  MonacoConfiguration
			.BootFromEndpoint<MsmqTransportEndpoint>(@"msmq.config");			
			wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
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
		public void can_use_bus_to_publish_a_message_to_a_consumer_on_msmq_endpoint()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<MsmqEndpointMessageConsumer>();
				bus.Start();

				for (int index = 1; index < 10; index++)
					bus.Publish(new MsmqMessage());

				wait.WaitOne(TimeSpan.FromSeconds(10));

				Assert.IsType<MsmqMessage>(received_message);
			}
		}

		[Fact]
		public void can_send_and_receive_message_from_msmq_queue()
		{
			var factory = this.configuration.Container.Resolve<IEndpointFactory>();
			var exchange = factory.Build(new Uri("msmq://localhost/local.service.bus"));

			exchange.Transport.IsRecoverable = false;
			exchange.Transport.Send(new Envelope(new MsmqMessage()));

			wait.WaitOne(TimeSpan.FromSeconds(5));
			wait.Set();

			var envelope = exchange.Transport.Receive(TimeSpan.FromSeconds(2));

			Assert.NotNull(envelope);
			Assert.IsType<MsmqMessage>(envelope.Body.Payload);
		}

		public class MsmqMessage : IMessage {}

		public class MsmqEndpointMessageConsumer : TransientConsumerOf<MsmqMessage>
		{
			public void Consume(MsmqMessage message)
			{
				received_message = message;
				received_count++;
				wait.Set();
				System.Diagnostics.Debug.WriteLine("Message received: " + received_count.ToString());
			}
		}
	}
}
