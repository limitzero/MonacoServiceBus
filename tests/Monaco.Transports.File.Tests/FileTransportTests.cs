using System;
using System.Threading;
using Castle.Windsor.Configuration.Interpreters;
using Monaco.Bus;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Xunit;

namespace Monaco.Transports.File.Tests
{
	// remember to mark the config file as an output to the executable directory !!!
	public class FileEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingFile())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly)
				  .SupportsTransactions(true));
		}
	}

	public class FileTransportTests : IDisposable
	{
		public static int received_count;
		public static ManualResetEvent wait;
		public static IMessage received_message;
		private MonacoConfiguration configuration;

		public FileTransportTests()
		{
			this.configuration = MonacoConfiguration
				.BootFromEndpoint<FileEndpointConfiguration>(new XmlInterpreter());

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
		public void can_read_settings_for_file_transport_from_configuration_section()
		{
			var settings = Monaco.Transports.File.Configuration.ConfigurationSectionHandler.GetConfiguration();
			Assert.Equal(".snd", settings.SendFileExtension);
			Assert.Equal(".rcv", settings.ReceiveFileExtension);
			Assert.Equal(".prd", settings.ProcessedFileExtension);
			Assert.Equal(true, settings.AutoDelete);
		}

		[Fact]
		public void can_use_bus_to_publish_a_message_to_a_consumer_on_file_endpoint()
		{
			using (var bus = this.configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<FileEndpointMessageConsumer>();
				bus.Start();

				for (int index = 1; index < 10; index++)
					bus.Publish(new FileMessage());

				wait.WaitOne(TimeSpan.FromSeconds(10));

				Assert.IsType<FileMessage>(received_message);
			}
		}

		[Fact]
		public void can_send_and_receive_message_from_file_queue()
		{
			var factory = this.configuration.Container.Resolve<IEndpointFactory>();
			var exchange = factory.Build(new Uri(@"file://c:\temp\local.service.bus"));

			exchange.Transport.IsRecoverable = false;
			exchange.Transport.Send(new Envelope(new FileMessage()));

			wait.WaitOne(TimeSpan.FromSeconds(5));
			wait.Set();

			var envelope = exchange.Transport.Receive(TimeSpan.FromSeconds(2));

			Assert.NotNull(envelope);
			Assert.IsType<FileMessage>(envelope.Body.Payload);
		}

		public class FileMessage : IMessage { }

		public class FileEndpointMessageConsumer : TransientConsumerOf<FileMessage>
		{
			public void Consume(FileMessage message)
			{
				received_message = message;
				received_count++;
				wait.Set();
				System.Diagnostics.Debug.WriteLine("Message received: " + received_count.ToString());
			}
		}
	}
}
