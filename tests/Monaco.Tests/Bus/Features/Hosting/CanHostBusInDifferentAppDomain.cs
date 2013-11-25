using System;
using System.Threading;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;
using Monaco.Configuration.Profiles;
using Monaco.Hosting;
using Monaco.Tests.Messages;
using Xunit;

namespace Monaco.Tests.Bus.Features.Hosting
{
	public class BusHostingTests : IDisposable,
		TransientConsumerOf<RemoteMessageReply>
	{
		private static IMessage _received_message;
		private MonacoConfiguration configuration;
		public static ManualResetEvent _wait;

		// starts bus with app.config settings in another domain:
		private RemoteAppDomainHost _host;

		public BusHostingTests()
		{
			// local bus instance:
			configuration = MonacoConfiguration
				.BootFromEndpoint<RemoteDomainHostEndpointConfig>(@"sample.config");
			_wait = new ManualResetEvent(false);

			// remote bus instance:
			_host = new RemoteAppDomainHost();
			_host.ConfigureWith(c => c.EndpointConfigurationOf<RemoteDomainHostEndpointConfig>()
				.ConfigurationFileNameOf("remote.bus.config"));
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;

			if (_wait != null)
			{
				_wait.Close();
			}
			_wait = null;

			if (_host != null)
			{
				_host.Dispose();
			}
			_host = null;
		}

		[Fact]
		public void can_host_bus_in_different_app_domain()
		{
			_host.Start();
			_wait.WaitOne(TimeSpan.FromSeconds(10));

			Assert.True(_host.IsRunning);
		}

		[Fact]
		public void can_host_bus_in_separate_app_domain()
		{
			_host.Start();

			var localhost = new DefaultHost();
			localhost.Configure(c => c.Bus("msmq://localhost/local.bus")
					.Error("msmq://localhost/error.queue")
					.Receive("Monaco.Tests.Messages", "")
				);
			localhost.Start("Monaco.Tests");

			// use remote bus to send message to local bus hosted on endpoint above:
			using (var bus = configuration.Resolve<IServiceBus>())
			using (bus.AddInstanceConsumer<BusHostingTests>())
			{	
					bus.Start();

					// send the message to the remote endpoint (in app config):
					bus.Send<RemoteMessage>(m => { });

					_wait.WaitOne(TimeSpan.FromSeconds(5));

					Assert.IsAssignableFrom<RemoteMessageReply>(_received_message);
			}
		}

		public void Consume(RemoteMessageReply message)
		{
			_received_message = message;
			_wait.Set();
		}
	}

	public class RemoteDomainHostEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapMessages<TestRemoteHandler>());
		}
	}

	public class TestRemoteHandler :
		Consumes<RemoteMessage>
	{
		private readonly IServiceBus _bus;

		public TestRemoteHandler(IServiceBus bus)
		{
			_bus = bus;
		}

		public void Consume(RemoteMessage message)
		{
			_bus.Reply(new RemoteMessageReply());
		}
	}
}