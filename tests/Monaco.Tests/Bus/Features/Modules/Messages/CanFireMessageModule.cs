using System;
using System.Threading;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Modules.Messages
{
	public class MessageModuleEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapMessageModule<BusWithMessageModuleTests.TestMessageModule>());
		}
	}

	public class BusWithMessageModuleTests 
	{
		public static ManualResetEvent Wait;
		public static int ModuleExecutedCount;
		public static IMessage ReceivedMessage;
		public static bool StartActionInvoked;
		public static bool EndActionInvoked;
		private MonacoConfiguration configuration;

		public BusWithMessageModuleTests()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint<MessageModuleEndpointConfig>(@"sample.config");
			Wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;

			if (Wait != null)
			{
				Wait.Close();
			}
			Wait = null;
		}

	/* A message module is run for every message that is received on the 
	 * bus from the bus endpoint just before the message is sent to the consumer
	 * for processing and just after the message has been processed at the consumer. 
	 * However, it will not be run for local admin messages flowing inside of the service bus, 
	 * this will allow the module to be executed only on user-defined messages.
	 */

		[Fact]
		public void can_fire_message_module_when_message_is_starting_the_receive_process_and_after_it_has_been_consumed()
		{
			using(var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<ModuleMessageConsumer>();

				bus.Start();

				bus.Publish(new MessageModuleMessage());

				Wait.WaitOne(TimeSpan.FromSeconds(5));
				Wait.Set();

				Assert.IsType<MessageModuleMessage>(ReceivedMessage);
				Assert.True(StartActionInvoked, "The start action on the message module could not be invoked.");
				Assert.True(EndActionInvoked, "The end action on the message module could not be invoked.");
			}
		}

		public class TestMessageModule : IMessageModule
		{
			public void Dispose()
			{
				// clean up here...
			}

			public void OnMessageStartProcessing(IContainer container, object message)
			{
				StartActionInvoked = true;
				System.Diagnostics.Debug.WriteLine("Starting message module..." + message.GetType().Name);
				ModuleExecutedCount++;
			}

			public void OnMessageEndProcessing(IContainer container, object message)
			{
				EndActionInvoked = true;
				System.Diagnostics.Debug.WriteLine("Ending message module..." + message.GetType().Name);
				ModuleExecutedCount++;
			}
		}

		public class ModuleMessageConsumer : TransientConsumerOf<MessageModuleMessage>
		{
			public void Consume(MessageModuleMessage message)
			{
				ReceivedMessage = message;
			}
		}
	}

	public class MessageModuleMessage : IMessage
	{}
}