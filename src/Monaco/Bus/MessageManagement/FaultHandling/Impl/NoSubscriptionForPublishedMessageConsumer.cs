using System;
using Monaco.Bus.Messages.For.Faults;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Extensibility.Logging;

namespace Monaco.Bus.MessageManagement.FaultHandling.Impl
{
	/// <summary>
	/// Fault handler to re-direct messages that do not have a subscription for publication 
	/// on the endpoint, when the centralized pub/sub engine is not present, to the error queue.
	/// </summary>
	public class NoSubscriptionForPublishedMessageConsumer :
		Consumes<NoSubscriptionForMessageFaultMessage>
	{
		private readonly IServiceBus bus;
		private readonly IServiceBusErrorEndpoint errorEndpoint;

		public IEnvelope Envelope { get; set; }
		public Exception Exception { get; set; }

		public NoSubscriptionForPublishedMessageConsumer(IServiceBus bus,
		                                                     IServiceBusErrorEndpoint errorEndpoint)
		{
			this.bus = bus;
			this.errorEndpoint = errorEndpoint;
		}
	
		public void Consume(NoSubscriptionForMessageFaultMessage message)
		{
			var theException =
				new Exception(string.Format("The current message '{0}' that is registered on the service bus could not " +
				                            "be delivered to the indicated endpoint via publish/subscribe. " +
				                            "Handling fault condition by moving the message to the '{1}' queue for offline inspection and/or recovery.",
				                            message.GetType().FullName,
				                            errorEndpoint.Endpoint.OriginalString));

			this.bus.Find<ILogger>().LogWarnMessage(theException.Message, theException);

			var envelope = new Envelope(message);
			envelope.Header.LocalEndpoint = this.bus.Endpoint.EndpointUri.OriginalString;
			envelope.Footer.RecordException(theException);

			this.bus.Send(errorEndpoint.Endpoint, envelope);
		}
	}
}