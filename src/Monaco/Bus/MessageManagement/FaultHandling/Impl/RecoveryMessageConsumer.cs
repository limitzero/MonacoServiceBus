using System;
using Monaco.Bus.Messages.For.Recovery;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Extensibility.Logging;
using Monaco.Extensions;
using Monaco.Transport;

namespace Monaco.Bus.MessageManagement.FaultHandling.Impl
{
	/// <summary>
	/// This fault handler will push the current message to the error 
	/// queue for possible recovery at a later point in time.
	/// </summary>
	public class RecoveryMessageConsumer
		: Consumes<RecoveryMessage>
	{
		private readonly IOneWayBus bus;
		private readonly IServiceBusErrorEndpoint errorEndpoint;
		private readonly ILogger logger;
		private readonly ITransport transport;

		public IEnvelope Envelope { get; set; }
		public Exception Exception { get; set; }

		public RecoveryMessageConsumer(IOneWayBus bus,
		                                                        ITransport transport,
		                                                        IServiceBusErrorEndpoint errorEndpoint,
		                                                        ILogger logger)
		{
			this.bus = bus;
			this.transport = transport;
			this.errorEndpoint = errorEndpoint;
			this.logger = logger;
		}

		public void Consume(RecoveryMessage message)
		{
			var theException =
				new Exception(string.Format("The current message '{0}' that is registered on the service bus at '{1}' could not " +
				                            "processed by the configured set of consumers due to an error. " +
				                            "Handling fault condition by moving the message to the '{2}' queue " +
				                            "for offline inspection and/or recovery.",
				                            message.Envelope.Body.Payload.ToItemList(),
				                            transport.Endpoint.EndpointUri,
				                            errorEndpoint.Endpoint.OriginalString));

			this.logger.LogWarnMessage(theException.Message, theException);
			message.Envelope.Footer.RecordException(theException);
			this.bus.Send(errorEndpoint.Endpoint, message);
		}
	}
}