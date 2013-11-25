using System;
using System.Collections.Generic;
using System.Threading;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.SendPublish
{	
	public class BusConsumerTestsEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapMessages<CanUseBusToProcessMessagesToConsumersTests.BusTestsLocalConsumer>());
		}
	}

	public class CanPublishMessagesToConsumers : IDisposable
	{
		public static ManualResetEvent _wait;
		public static IMessage _received_message;
		public static IMessage _batch_received_message1;
		public static IMessage _batch_received_message2;
		private MonacoConfiguration configuration;

		public CanPublishMessagesToConsumers()
		{
			this.configuration = MonacoConfiguration
				.BootFromEndpoint<BusConsumerTestsEndpointConfig>(@"sample.config");
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
		public void can_use_bus_to_publish_a_message_to_a_consumer()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SamplePublishedMessageConsumer>();
				bus.Start();

				bus.Publish(new PublishedMessage1());
				_wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsType<PublishedMessage1>(_received_message);
			}
		}

		[Fact]
		public void can_use_bus_to_publish_message_batch_to_consumers()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SamplePublishedMessageConsumer>();
				bus.Start();

				bus.Publish(new PublishBatchMessage1(),
				            new PublishBatchMessage2());

				_wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsType<PublishBatchMessage1>(_batch_received_message1);
				Assert.IsType<PublishBatchMessage2>(_batch_received_message2);
			}

		}

		public class PublishedMessage1 : IMessage
		{
			public Guid CorrelationId { get; set; }
		}

		public class PublishBatchMessage1 : IMessage
		{
			public Guid CorrelationId { get; set; }
		}

		public class PublishBatchMessage2 : IMessage
		{
			public Guid CorrelationId { get; set; }
		}

		public class SamplePublishedMessageConsumer : 
			TransientConsumerOf<PublishedMessage1>,
			TransientConsumerOf<PublishBatchMessage1>,
			TransientConsumerOf<PublishBatchMessage2>
		{
			public void Consume(PublishedMessage1 message)
			{
				_received_message = message;
				_wait.Set();
			}

			public void Consume(PublishBatchMessage1 message)
			{
				_batch_received_message1 = message;
			}

			public void Consume(PublishBatchMessage2 message)
			{
				_batch_received_message2 = message;
				_wait.Set();
			}
		}

	}

	public class CanUseBusToProcessMessagesToConsumersTests : IDisposable
	{
		public static ManualResetEvent Wait;
		public static List<IMessage> ReceivedMessages;
		public static IMessage ReceivedMessage;
		private MonacoConfiguration configuration;

		public CanUseBusToProcessMessagesToConsumersTests()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint<BusConsumerTestsEndpointConfig>(@"sample.config");

			Wait = new ManualResetEvent(false);
			ReceivedMessages = new List<IMessage>();
		}

		public void Dispose()
		{
			ReceivedMessages.Clear();
			ReceivedMessages = null;

			configuration.Dispose();
			configuration = null;

			if (Wait != null)
			{
				Wait.Close();
				Wait = null;
			}
		}

		[Fact]
		public void can_send_message_to_consumer_defined_from_endpoint_configuration()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				bus.Send(new BusConsumerTestsMessage1());

				Wait.WaitOne(TimeSpan.FromSeconds(5));
				Wait.Reset();

				Assert.True(Utilities.IsMessageReceived<BusConsumerTestsMessage1>(ReceivedMessages));
			}
		}

		[Fact]
		public void can_publish_message_to_consumer_defined_from_endpoint_configuration()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				bus.Publish(new BusConsumerTestsMessage1());

				Wait.WaitOne(TimeSpan.FromSeconds(5));
				Wait.Reset();

				Assert.True(Utilities.IsMessageReceived<BusConsumerTestsMessage1>(ReceivedMessages));
			}
		}

		[Fact]
		public void can_publish_message_batch_to_consumer_from_endpoint_configuration()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				//TODO: for this, the envelope needs to have the abilty to store multiple messages:
				bus.Publish(new BatchMessage1(), 
				                   new BatchMessage2());

				Wait.WaitOne(TimeSpan.FromSeconds(5));
				Wait.Set();

				Assert.True(Utilities.IsMessageReceived<BatchMessage1>(ReceivedMessages),
					"The first batch message could not be retreived.");

				Assert.True(Utilities.IsMessageReceived<BatchMessage2>(ReceivedMessages),
					"The second batch message could not be retrieved.");
			}
		}

		[Fact]
		public void can_publish_scheduled_timeout_message_to_consumer_after_duration_has_passed()
		{
			int duration = 2;

			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				var st = new ScheduleTimeout(TimeSpan.FromSeconds(duration), new DelayedMessage());
				st.Endpoint = bus.Endpoint.EndpointUri.ToString();

				bus.AddInstanceConsumer<BusTestsLocalConsumer>();

				bus.Start();

				bus.Send(st);

				Wait.WaitOne(TimeSpan.FromSeconds(duration * 2));

				Assert.IsType<DelayedMessage>(ReceivedMessage);
			}
		}

		[Fact]
		public void can_send_message_to_dsl_message_consumer()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<DSLMessageConsumer>();
				bus.Start();

				bus.Send(new MyRequest());

				Wait.WaitOne(TimeSpan.FromSeconds(5));
				
				Assert.IsType<MyRequest>(ReceivedMessage);
			}
		}


		public class BatchMessage1 : IMessage
		{
		}

		public class BatchMessage2 : IMessage
		{
		}

		public class DelayedMessage : IMessage
		{
		}

		public interface IIBMTrade : ITrade
		{
		}

		public interface ITrade : IMessage
		{
			int TradeId { get; set; }
			string StockSymbol { get; set; }
		}

		public class BusTestsLocalConsumer :
			TransientConsumerOf<BusConsumerTestsMessage1>,
			TransientConsumerOf<BatchMessage1>,
			TransientConsumerOf<BatchMessage2>,
			TransientConsumerOf<DelayedMessage>,
			Consumes<IIBMTrade>,
			TransientConsumerOf<MyRequest>
		{
			private readonly IServiceBus _bus;
			

			public BusTestsLocalConsumer(IServiceBus bus)
			{
				this._bus = bus;
			}

			public void Consume(BatchMessage1 message)
			{
				ReceivedMessages.Add(message);
			}

			public void Consume(BatchMessage2 message)
			{
				ReceivedMessage = message;
			}

			public void Consume(DelayedMessage message)
			{
				ReceivedMessage = message;
				Wait.Set();
			}

			public void Consume(IIBMTrade message)
			{
				ReceivedMessages.Add(message);
				Wait.Set();
			}

			public void Consume(BusConsumerTestsMessage1 message)
			{
				System.Diagnostics.Debug.WriteLine("At " + GetType().FullName);
				ReceivedMessages.Add(message);
				Wait.Set();
			}

			public void Consume(MyRequest message)
			{
				var response = new MyResponse();
				response.Data = message.Data;

				_bus.Reply(response);
				Wait.Set();
			}
		}


		public class DSLMessageConsumer : MessageConsumer, 
			TransientConsumerOf<MyRequest>
		{
			public override void Define()
			{
				UponReceiving<MyRequest>( message => 
					       {
								ReceivedMessage = message;
								Wait.Set();
					    	});
			}

			public void Consume(MyRequest message)
			{
			
			}
		}

		public class BusConsumerTestsMessage1 : IMessage
		{
		}

		public class MyRequest : IMessage
		{
			public string Data { get; set; }
		}

		public class MyResponse : IMessage
		{
			public string Data { get; set; }
		}
	}
}