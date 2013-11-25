using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;
using Monaco.Configuration.Profiles;
using Monaco.StateMachine;
using Xunit;

namespace Monaco.Tests
{

	public class PerformaceSagaEndpoint : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint( e=> e.MapMessages<PerformanceTests.PerformanceStateMachine>()
					.MapMessages<PerformanceTests.PerformanceConsumer>());
		}
	}

	public class PerformanceTests
	{
		public static ManualResetEvent _wait;
		public static List<object> _received_messages;
		private MonacoConfiguration _container;

		public PerformanceTests()
		{
			_container = new MonacoConfiguration(@"sample.config");
			_received_messages =new List<object>();
		}

		public void Dispose()
		{
			_received_messages = null;
			_container.Dispose();
			_container = null;
		}

		[Fact]
		public void can_publish_1000_messages_to_consumer()
		{
			var numberOfMessages = 1000;

			using (IServiceBus bus = _container.Resolve<IServiceBus>())
			{
				bus.ConfiguredWithEndpoint<PerformaceSagaEndpoint>();

				bus.Start();

				var token = bus.AddInstanceConsumer<PerformanceConsumer>();

				System.Diagnostics.Stopwatch sw = new Stopwatch();
				sw.Start();

				for (int index = 1; index < numberOfMessages; index++)
				{
					bus.Publish(new PerformanceMessage());
				}

				sw.Stop();

				token.Dispose();

				System.Diagnostics.Debug.WriteLine("Elapsed Seconds: " + sw.Elapsed.Seconds.ToString());
				System.Diagnostics.Debug.WriteLine("Messages per second:" +
												   MessagesPerSecond(numberOfMessages, sw.Elapsed.Seconds).ToString("G"));
			}
		}

		[Fact]
		public void can_publish_1000_messages_to_saga()
		{
			var numberOfMessages = 1000;

			using (IServiceBus bus = _container.Resolve<IServiceBus>())
			{
				bus.ConfiguredWithEndpoint<PerformaceSagaEndpoint>();

				bus.Start();

				System.Diagnostics.Stopwatch sw = new Stopwatch();
				sw.Start();

				for (int index = 1; index < numberOfMessages; index++)
				{
					bus.Publish(new PerformanceSagaMessage1());
				}

				sw.Stop();

				System.Diagnostics.Debug.WriteLine("Elapsed Seconds: " + sw.Elapsed.Seconds.ToString());
				System.Diagnostics.Debug.WriteLine("Messages per second:" +
					MessagesPerSecond(_received_messages.Count, sw.Elapsed.Seconds).ToString("G"));

			}
		}

		private static decimal MessagesPerSecond(int numberOfMessages, int numberOfSeconds)
		{
			decimal messages = decimal.Zero;
			decimal seconds = decimal.Zero;

			decimal.TryParse(numberOfMessages.ToString(), out messages);
			decimal.TryParse(numberOfSeconds.ToString(), out seconds);

			return messages / seconds;
		}

		public class PerformanceConsumer : 
			TransientConsumerOf<PerformanceMessage>
		{
			public void Consume(PerformanceMessage message)
			{
				_received_messages.Add(message);
			}
		}

		public class PerformanceStateMachine : 
	    SagaStateMachine<PerformanceStateMachineData>,
		StartedBy<PerformanceSagaMessage1>,
		OrchestratedBy<PerformanceSagaMessage2>
		{
			public Event<PerformanceSagaMessage1> FirstMessageReceived { get; set; }
			public Event<PerformanceSagaMessage2> SecondMessageReceived { get; set; }

			public void Consume(PerformanceSagaMessage1 message)
			{
			}

			public void Consume(PerformanceSagaMessage2 message)
			{
			}

			public override void Define()
			{
				Initially(
					When(FirstMessageReceived)
					.Do((message)=>
					    	{
								_received_messages.Add(message);

								// need to have the saga persisted in order to 
								// find out what the storage costs are in processing 
								// a message:
								Bus.Publish(new PerformanceSagaMessage2());
					    	})
					);

				Also(
					When(SecondMessageReceived)
						.Complete()
					);
			}
		}

	}

	public class PerformanceMessage : IMessage
	{
		public Guid CorrelationId { get; set; }
	}

	public class PerformanceSagaMessage2 : IMessage
	{
		public Guid CorrelationId { get; set; }
	}

	public class PerformanceSagaMessage1 : IMessage
	{
		public Guid CorrelationId { get; set; }
	}

	public class PerformanceStateMachineData : IStateMachineData
	{
		public virtual Guid Id { get; set; }
		public virtual string State { get; set; }
		public virtual int Version { get; set; }
	}
}