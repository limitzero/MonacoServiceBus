using System;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.SendPublish
{
	public class SendMessagesEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithEndpointName("sender")
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapMessages<CanSendMessagesToConsumers.SampleSendMessageConsumer>()
				 .SupportsTransactions(true));
		}
	}

	public class CanSendMessagesToConsumers : IDisposable
	{
		public static ManualResetEvent wait;
		public static IMessage receivedMessage;
		public static IMessage batchReceivedMessage1;
		public static IMessage batchReceivedMessage2;
		private MonacoConfiguration configuration;

		public CanSendMessagesToConsumers()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<SendMessagesEndpointConfig>(@"sample.config");
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
				wait = null;
			}
		}

		[Fact]
		public void can_use_bus_to_send_a_message_to_a_consumer()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SampleSendMessageConsumer>();
				bus.Start();

				bus.Send<FirstSendMessage>(m =>
				                           	{
				                           		m.CorrelationId = Guid.NewGuid();
				                           	});

				wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsType<FirstSendMessage>(receivedMessage);
			}
		}

		[Fact]
		public void can_use_bus_to_send_message_batch_to_consumers()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SampleSendMessageConsumer>();
				bus.Start();

				bus.Send(new SendBatchMessage1(),
							   new SendBatchMessage2());

				wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.IsAssignableFrom<SendBatchMessage1>(batchReceivedMessage1);
				Assert.IsAssignableFrom<SendBatchMessage2>(batchReceivedMessage2);
			}

		}

		[Fact(Skip="Need to refine or deprecate wait handles on send w/callback")]
		public void can_use_bus_to_send_message_and_use_callback_to_get_reply_message()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SampleSendMessageConsumer>();
				bus.Start();

				var request = bus.EnqueueRequest();

				request.WithCallback(() => Console.WriteLine("Reply being delivered for request : " + typeof (RequestMessage).Name))
					.Send(new RequestMessage());

				var response = request.GetReply<ResponseMessage>();

				Assert.IsType<ResponseMessage>(response);
			}
		}

		public class FirstSendMessage : IMessage
		{
			public Guid CorrelationId { get; set; }
		}

		public class RequestMessage : IMessage
		{}

		public class ResponseMessage : IMessage
		{}

		public class SendBatchMessage1 : IMessage
		{
		}

		public class SendBatchMessage2 : IMessage
		{
		}

		public class SampleSendMessageConsumer :
			TransientConsumerOf<FirstSendMessage>,
			TransientConsumerOf<SendBatchMessage1>,
			TransientConsumerOf<SendBatchMessage2>,
			TransientConsumerOf<RequestMessage>,
			TransientConsumerOf<ResponseMessage>
		{
			private readonly IServiceBus _bus;

			public SampleSendMessageConsumer(IServiceBus bus)
			{
				_bus = bus;
			}

			public void Consume(FirstSendMessage message)
			{
				receivedMessage = message;
				wait.Set();
			}

			public void Consume(SendBatchMessage1 message)
			{
				batchReceivedMessage1 = message;
			}

			public void Consume(SendBatchMessage2 message)
			{
				batchReceivedMessage2 = message;
				wait.Set();
			}

			public void Consume(RequestMessage message)
			{
				_bus.Reply(new ResponseMessage());				
			}

			public void Consume(ResponseMessage message)
			{
				receivedMessage = message;
				wait.Set();
			}
		}

	}
}