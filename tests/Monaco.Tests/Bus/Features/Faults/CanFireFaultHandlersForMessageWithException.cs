using System;
using System.Threading;
using Monaco.Bus;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;
using Monaco.Configuration.Profiles;
using Xunit;

namespace Monaco.Tests.Bus.Features.Faults
{
	public class FaultHandlerErrorMessage : IMessage { }

	public class FaultHandlerEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
			.WithContainer(c => c.UsingWindsor())
			.WithStorage(s => s.UsingInMemoryStorage())
			.WithTransport(t => t.UsingMsmq())
				// forcibly map a message to a fault handler for special processing when an exception happens:
			.WithEndpoint(e => e.ConfigureMessageFaultHandlerChain(c => c.ForMessage<FaultHandlerErrorMessage>()
				.WithHandler<CanUseBusToFireFaultHandlersForMessageWithException.FaultHandlerForFaultHandlerErrorMessage>()));
		}
	}

	public class CanUseBusToFireFaultHandlersForMessageWithException : IDisposable,
		TransientConsumerOf<FaultHandlerErrorMessage>
	{
		public static ManualResetEvent Wait;
		public static IMessage ReceivedMessage;
		public static bool IsFaultHandlerFired;
		private MonacoConfiguration configuration;

		public CanUseBusToFireFaultHandlersForMessageWithException()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint < FaultHandlerEndpointConfig>(@"sample.config");
			Wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			configuration.Dispose();
			configuration = null;

			if (Wait != null)
			{
				Wait.Close();
			}
			Wait = null;
		}

		[Fact]
		public void can_use_bus_to_fire_configured_fault_handler_from_endpoint_configuration_when_message_generates_error_after_retries_are_exceeded()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				var unsubscribe =
				   bus.AddInstanceConsumer<CanUseBusToFireFaultHandlersForMessageWithException>();

				bus.Start();

				bus.Send(new FaultHandlerErrorMessage());
				Wait.WaitOne(TimeSpan.FromSeconds(10));

				// let's be good citizens and dispose of the transient instances:
				unsubscribe.Dispose();

				Assert.True(IsFaultHandlerFired, "The fault handler was not fired for the message");
			}
		}

		public void Consume(FaultHandlerErrorMessage message)
		{
			// this generates the exception to force the fault handlers to run:
			throw new NotImplementedException("This is not implemented.");
		}

		public class FaultHandlerForFaultHandlerErrorMessage : FaultConsumer<FaultHandlerErrorMessage>
		{
			private readonly IOneWayBus bus;

			public IEnvelope Envelope { get; set; }
			public Exception Exception { get; set; }

			public FaultHandlerForFaultHandlerErrorMessage(IOneWayBus  bus)
			{
				this.bus = bus;
			}

			public void Consume(FaultHandlerErrorMessage message)
			{
				System.Diagnostics.Debug.WriteLine("Running fault handler...");
				// can delay the message for a period after the default retry period:
				//_bus.HandleMessageLater(TimeSpan.FromMinutes(10), faultMessage.Message);
				IsFaultHandlerFired = true;
				Wait.Set();
			}


		}


	}
}