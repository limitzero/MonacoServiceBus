using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals;
using Monaco.Bus.MessageManagement.Dispatcher.Internal;
using Monaco.Bus.MessageManagement.Resolving;
using Monaco.Bus.Messages.For.Recovery;
using Monaco.Configuration;
using Monaco.Extensibility.Logging;
using Monaco.Transport;

namespace Monaco.Bus.MessageManagement.FaultHandling
{
	/// <summary>
	/// Processor to invoke all fault handlers for a given message:
	/// </summary>
	public class FaultProcessor : IFaultProcessor
	{
		private readonly IServiceBus serviceBus;

		public FaultProcessor(IServiceBus serviceBus)
		{
			this.serviceBus = serviceBus;
		}

		#region IFaultProcessor Members

		public void Process<TMessage>(TMessage message, IEnvelope envelope = null, Exception exception = null)
		{
			IEnumerable<IConsumer> consumers = null;
			var logger = serviceBus.Find<ILogger>();

			if (envelope == null)
			{
				envelope = new Envelope(message);
			}

			var resolver = this.serviceBus.Find<IResolveMessageToConsumers>();

			// find the fault handlers created for the specific message and excersice them:
			try
			{
				consumers = resolver.ResolveAll(envelope);
			}
			catch
			{
			}

			if (consumers == null || consumers.Count() == 0)
			{	
				// no fault handler configured for the specific message, must change this to a recoverable message:
				logger.LogWarnMessage(
					string.Format(
						"No custom fault handler was found for message '{0}'. Changing the message to a recovery message for standard exception handling.",
						envelope.Body.Payload.GetType().FullName));

				// no fault handlers for message, must push message to error location:
				// (create the message for possible recovery at a later time and invoke the standard fault handler)
				var recoveryMessage = new RecoveryMessage {Envelope = envelope, OccuredAt = System.DateTime.Now};
				this.serviceBus.ConsumeMessages(recoveryMessage);
				return;
			}
			
			// exercise all of the fault handlers for the message:
			foreach (IConsumer consumer in consumers)
			{
				try
				{
					logger.LogDebugMessage(string.Format("Executing custom fault handler '{0}' for message '{1}'.",
					                                     consumer.GetType().FullName, message.GetType().FullName));

					envelope.Header.RecordStage(consumer, message, "Fault Handler");

					var method = new MessageToMethodMapper().Map(consumer, message);
					var invoker = new MessageMethodInvoker().Invoke(consumer, method, message);
				}
				catch (Exception faultHandlerException)
				{
					// log the error for the fault handler and continue:
					logger.LogWarnMessage(
						string.Format(
							"An error has occurred while attempting to execute the custom fault handler '{0}' for message '{1}'. Reason: {2}",
							consumer.GetType().FullName,
							message.GetType().FullName,
							faultHandlerException.Message), faultHandlerException);
					continue;
				}
			}
		}

		#endregion
	}
}