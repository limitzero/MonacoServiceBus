using System;
using Monaco.Bus.Messages.For.Faults;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Extensibility.Logging;

namespace Monaco.Bus.MessageManagement.FaultHandling.Impl
{
	/// <summary>
	/// Fault handler to re-direct messages that do not have a matching consumer 
	/// instance onto the endpoint to the error queue.
	/// </summary>
	public class NoConsumerForMessageFaultHandler :
		Consumes<NoConsumerForMessageFaultMessage>
	{
		private readonly IServiceBusErrorEndpoint _errorEndpoint;
		private readonly ILogger _logger;
		private readonly IOneWayBus _oneWayBus;
		private readonly IServiceBus _serviceBus;

		public NoConsumerForMessageFaultHandler(IOneWayBus oneWayBus,
		                                        IServiceBus serviceBus,
		                                        IServiceBusErrorEndpoint errorEndpoint,
		                                        ILogger logger)
		{
			_oneWayBus = oneWayBus;
			_serviceBus = serviceBus;
			_errorEndpoint = errorEndpoint;
			_logger = logger;
		}

		#region FaultConsumer<NoConsumerForMessageFaultMessage> Members

		public IEnvelope Envelope { get; set; }
		public Exception Exception { get; set; }

		public void Consume(NoConsumerForMessageFaultMessage message)
		{
			var theException =
				new Exception(string.Format("The current message '{0}' that is registered on the service bus at '{1}' could not " +
				                            "be mapped to a message consumer for processing. " +
				                            "Handling fault condition by moving the message to the '{2}' queue " +
				                            "for offline inspection and/or recovery.",
				                            message.Message.GetType().FullName,
											_serviceBus.Endpoint.EndpointUri.OriginalString,
				                            _errorEndpoint.Endpoint.OriginalString));

			_logger.LogWarnMessage(theException.Message, theException);

			Envelope.Footer.RecordException(theException);

			_oneWayBus.Send(_errorEndpoint.Endpoint, Envelope);
		}

		#endregion
	}
}