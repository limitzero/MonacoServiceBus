using System;
using System.Threading;
using System.Transactions;
using Monaco.Bus;
using Monaco.Bus.Messages.For.Recovery;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Impl.Bus;
using Xunit;

namespace Monaco.Tests.Bus.Features.Faults
{
	public class MappedMessageForConsumption : IMessage { }

	public class NonMappedMessageForConsumption : IMessage { }

	public class NonMappedMessageToConsumerEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.SupportsTransactions(true));
		}
	}

	public class CanUseBusToFireFaultHandlerForMessagesWithNoConsumer : IDisposable,
		TransientConsumerOf<MappedMessageForConsumption>
	{
		public static ManualResetEvent _wait;
		public static IMessage _receivedMessage;
		public static bool _isFaultHandlerFired;
		private MonacoConfiguration configuration;

		public CanUseBusToFireFaultHandlerForMessagesWithNoConsumer()
		{
			configuration = MonacoConfiguration
			     .BootFromEndpoint<NonMappedMessageToConsumerEndpointConfig>(@"sample.config");
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
			}
			_wait = null;
		}

		public void Consume(MappedMessageForConsumption message)
		{

		}

		[Fact]
		public void can_use_bus_to_fire_non_mapped_message_to_consumer_fault_handler_and_move_message_to_error_endpoint()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				// create transport on msmq locaton for testing (need persistance based transport for this test):
				var endpointFactory = configuration.Container.Resolve<IEndpointFactory>();
				var exchange = endpointFactory.Build(bus.Endpoint.EndpointUri);
				var msmqTransport = exchange.Transport;
				msmqTransport.IsRecoverable = false; // clear the contents (for testing only)

				// send a message to the bus that is not mapped to any consumer:
				exchange.Transport.Send(new Envelope(new NonMappedMessageForConsumption()));

				var errorEndpoint = this.configuration.Container.Resolve<IServiceBusErrorEndpoint>();
				var errorExchange = endpointFactory.Build(errorEndpoint.Endpoint);
				errorExchange.Transport.IsRecoverable = false;

				bus.Start();

				_wait.WaitOne(TimeSpan.FromSeconds(10));
				_wait.Set();

				// extract the message back from the configured error queue:
				using (var txn = new TransactionScope())
				{
					var envelope = errorExchange.Transport.Receive(TimeSpan.FromSeconds(5));

					Assert.True(envelope != null,
					            "The fault handler could not push the non-mapped message to the error endpoint.");

					Assert.IsType<NonMappedMessageForConsumption>(envelope.Body.GetPayload<RecoveryMessage>()
						.Envelope.Body.GetPayload<NonMappedMessageForConsumption>());

					txn.Complete();
				}

			}


		}
	}
}