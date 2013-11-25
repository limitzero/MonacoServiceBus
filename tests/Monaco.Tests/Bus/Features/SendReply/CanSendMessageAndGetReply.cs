using System;
using System.Collections.Generic;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.SendReply
{
	public class SendAndReplyEndpointConfiguration : ICanConfigureEndpoint
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

	public class CanUseBusToSendMessageAndGetReply : IDisposable, 
		TransientConsumerOf<CanUseBusToSendMessageAndGetReply.PongMessage>
	{
		public static ManualResetEvent wait;
		public static List<IMessage> receivedMessages;
		private static IMessage replyMessage;
		private MonacoConfiguration configuration;

		public CanUseBusToSendMessageAndGetReply()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<SendAndReplyEndpointConfiguration>(@"sample.config"); 
			wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			if(receivedMessages != null)
			{
				receivedMessages.Clear();
			}
			receivedMessages = null; 

			if(configuration != null)
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
		public void can_send_a_message_to_a_consumer_and_send_reply_to_another_consumer_on_bus_instance()
		{
			using(var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<PingMessageConsumer>();
				bus.AddInstanceConsumer(this);
				
				bus.Start();

				bus.Send(new PingMessage());

				wait.WaitOne(TimeSpan.FromSeconds(5));
				
				Assert.IsAssignableFrom<PongMessage>(replyMessage);
			}

		}

		public void Consume(PongMessage message)
		{
			replyMessage = message;
			wait.Set();
		}

		public class PingMessage : IMessage { }

		public class PongMessage : IMessage { }

		public class PingMessageConsumer : 
			TransientConsumerOf<PingMessage>
		{
			private readonly IServiceBus bus;

			public PingMessageConsumer(IServiceBus bus)
			{
				this.bus = bus;
			}

			public void Consume(PingMessage message)
			{
				this.bus.Reply(new PongMessage());
			
			}
		}

	}



}