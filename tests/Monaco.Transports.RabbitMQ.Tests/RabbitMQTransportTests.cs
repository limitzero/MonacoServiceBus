using System;
using System.Threading;
using Castle.Windsor.Configuration.Interpreters;
using Monaco.Bus;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Xunit;

namespace Monaco.Transports.RabbitMQ.Tests
{
	public class RabbitMQEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(s => s.UsingRabbitMQ())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly));
		}
	}

	public class RabbitMQTransportTests : IDisposable
	{
		public static int received_count;
		public static ManualResetEvent wait;
		public static IMessage received_message;
		private MonacoConfiguration configuration;

		public RabbitMQTransportTests()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<RabbitMQEndpointConfiguration>(new XmlInterpreter());
			
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
		public void can_read_transport_settings_from_local_application_configuration_file()
		{
			var settings = Monaco.Transports.RabbitMQ.Configuration.ConfigurationSectionHandler.GetConfiguration();
			Assert.Equal("rabbit_user", settings.UserName);
			Assert.Equal("rabbit_pwd", settings.Password);
			Assert.Equal("rabbit_host", settings.Host);
			Assert.Equal("rabbit_exchange", settings.Exchange);
			Assert.Equal(100, settings.Port);
			Assert.Equal("AMQP_0_9_1", settings.Protocol);
		}

		[Fact]
		public void can_use_bus_to_publish_a_message_to_a_consumer_on_rabbitMQ_endpoint()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<RabbitMQEndpointMessageConsumer>();
				bus.Start();

				for (int index = 1; index <= 10; index++)
					bus.Publish(new RabbitMQMessage());

				wait.WaitOne(TimeSpan.FromSeconds(10));
				wait.Set();

				Assert.Equal(10, received_count);
			}
		}

		[Fact]
		public void can_send_and_receive_message_from_rabbitMQ_queue()
		{
			var factory = configuration.Container.Resolve<IEndpointFactory>();
			var exchange = factory.Build(new Uri("rabbitmq://local.bus"));

			exchange.Transport.IsRecoverable = false;
			exchange.Transport.Send(new Envelope(new RabbitMQMessage()));

			wait.WaitOne(TimeSpan.FromSeconds(5));
			wait.Set();

			var envelope = exchange.Transport.Receive(TimeSpan.FromSeconds(2));

			Assert.NotNull(envelope);
			Assert.IsType<RabbitMQMessage>(envelope.Body.Payload);
		}

		public class RabbitMQMessage : IMessage
		{
		}

		public class RabbitMQEndpointMessageConsumer
			: TransientConsumerOf<RabbitMQMessage>
		{
			public void Consume(RabbitMQMessage message)
			{
				received_message = message;
				received_count++;
				System.Diagnostics.Debug.WriteLine("Message received: " + received_count.ToString());
			}
		}
	}
}
