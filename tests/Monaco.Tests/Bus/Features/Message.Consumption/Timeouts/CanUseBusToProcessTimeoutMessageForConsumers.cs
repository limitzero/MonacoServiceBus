using System;
using System.Threading;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Configuration;
using Xunit;

namespace Monaco.Tests.Bus.Features.Message.Consumption.Timeouts
{
	public class SagaSupportEndpointConfig : ICanConfigureEndpoint
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

	public class CanUseBusToProcessTimeoutMessageForConsumers : IDisposable
	{
		public static ManualResetEvent Wait;
		public static IMessage ReceivedMessage;
		private MonacoConfiguration configuration;

		public CanUseBusToProcessTimeoutMessageForConsumers()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<SagaSupportEndpointConfig>(@"sample.config");
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

		[Fact]
		public void can_use_bus_to_indicate_that_it_will_be_handled_at_later_time_by_consumer()
		{
			int delayPeriod = 5;

			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SampleDelayedMessageConsumer>();
			
				bus.Start();

				bus.Defer(TimeSpan.FromSeconds(delayPeriod), new DelayedMessage());
				Wait.WaitOne(TimeSpan.FromSeconds(delayPeriod * 2));
				
				Assert.IsType<DelayedMessage>(ReceivedMessage);
			}
		}

		[Fact]
		public void can_schedule_a_timeout_for_message_for_delivery_at_a_later_time_to_consumer()
		{
			int delayPeriod = 5;

			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.AddInstanceConsumer<SampleDelayedMessageConsumer>();
			
				bus.Start();

				var timeout = new ScheduleTimeout(TimeSpan.FromSeconds(delayPeriod),
					new DelayedMessage());

				bus.Send(timeout);

				Wait.WaitOne(TimeSpan.FromSeconds(10));

				Assert.IsType<DelayedMessage>(ReceivedMessage);
			}
		}

		public class DelayedMessage : IMessage
		{ }

		public class SampleDelayedMessageConsumer :
			TransientConsumerOf<DelayedMessage>
		{
			public void Consume(DelayedMessage message)
			{
				ReceivedMessage = message;
				Wait.Set();
			}
		}

	}
}