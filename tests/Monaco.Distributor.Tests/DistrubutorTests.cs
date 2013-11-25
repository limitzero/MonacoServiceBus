using System;
using System.Threading;
using Monaco.Configuration;
using Monaco.Distributor.Configuration;
using Monaco.StateMachine;
using Xunit;

namespace Monaco.Distributor.Tests
{
	public class DistributorEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.SupportsTransactions(true))
				.UsingDistributor();
		}
	}

	//http://stackoverflow.com/questions/3877294/writing-a-weighted-load-balancing-algorithm?rq=1
	public class DistributorTests : IDisposable
	{
		public static ManualResetEvent _wait;
		public static IMessage _received_message;
		public static IMessage _batch_received_message1;
		public static IMessage _batch_received_message2;
		private MonacoDistributorConfiguration configuration;

		public DistributorTests()
		{
			configuration =  new MonacoDistributorConfiguration(@"distributor.config");
			_wait = new ManualResetEvent(false);
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
				_wait = null;
			}
		}

		[Fact]
		public void can_start_distributor_and_push_messages_out_to_worker_in_worker_pool()
		{
			using (var distributor = configuration.Container.Resolve<IDistributor>())
			{
				var bus = configuration.Container.Resolve<IOneWayBus>();

				for (int index = 0; index < 100; index++)
				{
					bus.Send(new Uri("msmq://localhost/load.balanced.endpoint.1"), new FabricMessage
					{
						Index = index
					});
					//bus.Send(new Uri("msmq://localhost/load.balanced.endpoint.2"), new FabricMessage());
				}

				distributor.Start();
				_wait.WaitOne(TimeSpan.FromSeconds(10));
				_wait.Set();
			}
		}

		public class FabricMessage : IMessage
		{
			public int Index { get; set; }
		}
	}
}