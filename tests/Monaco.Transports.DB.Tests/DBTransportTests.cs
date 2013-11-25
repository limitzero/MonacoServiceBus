using System;
using System.Threading;
using Castle.Windsor.Configuration.Interpreters;
using Monaco.Bus;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Xunit;

namespace Monaco.Transports.DB.Tests
{
	// remember to mark the log4Net and db.transport configuration 
	// files copied to the output directory for the logging and settings 
	// to be read at runtime:
	public class DBEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithTransport(t => t.UsingDB())
				.WithStorage(s => s.UsingInMemoryStorage())
				// this mapping option does not pick-up transient types, use bus.AddInstanceConsumer<> for that:
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly)
				 .SupportsTransactions(true));
		}
	}

	public class DBTransportTests : IDisposable
	{
		public static int received_count;
		public static ManualResetEvent wait;
		public static IMessage received_message;
		private MonacoConfiguration configuration;

		public DBTransportTests()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint<DBEndpointConfiguration>(new XmlInterpreter());
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
		public void can_read_settings_for_sql_transport_from_configuration_section()
		{
			var settings = Monaco.Transports.DB.Configuration.ConfigurationSectionHandler.GetConfiguration();
			Assert.Equal(string.Empty, settings.UserName);
			Assert.Equal(string.Empty, settings.Password);
			Assert.Equal(@".\SqlExpress", settings.Server);
			Assert.Equal("monaco", settings.Catalog);
			Assert.Equal(true, settings.AutoDelete);
		}

		[Fact]
		public void can_use_bus_to_publish_a_message_to_a_consumer_on_sql_endpoint()
		{
			using (var bus = this.configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SqlDBEndpointMessageConsumer>();
				bus.Start();

				for (int index = 1; index <= 10; index++)
					bus.Publish(new SqlDBMessage());

				wait.WaitOne(TimeSpan.FromSeconds(10));
				wait.Set();
				Assert.Equal(10, received_count);
			}
		}

		[Fact]
		public void can_send_and_receive_message_from_sql_db_queue()
		{
			var factory = this.configuration.Container.Resolve<IEndpointFactory>();
			var exchange = factory.Build(new Uri("sqldb://local.bus"));

			exchange.Transport.IsRecoverable = false;
			exchange.Transport.Send(new Envelope(new SqlDBMessage()));

			wait.WaitOne(TimeSpan.FromSeconds(5));
			wait.Set();

			var envelope = exchange.Transport.Receive(TimeSpan.FromSeconds(2));

			Assert.NotNull(envelope);
			Assert.IsType<SqlDBMessage>(envelope.Body.Payload);
		}

		public class SqlDBMessage : IMessage
		{
		}

		public class SqlDBEndpointMessageConsumer
			: TransientConsumerOf<SqlDBMessage>
		{
			public void Consume(SqlDBMessage message)
			{
				received_message = message;
				received_count++;
				System.Diagnostics.Debug.WriteLine("Message received: " + received_count.ToString());
			}
		}
	}
}
