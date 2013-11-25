using System;
using System.Collections.Generic;

namespace Monaco.Bus.MessageManagement.FaultHandling
{
	/// <summary>
	/// Configuration for setting fault handlers for messages that need additional processing 
	/// logic if and/or when the initial processing of the message fails at the consumer.
	/// <code>
	/// var cfg = new FaultHandlerConfiguration()
	///		.ForMessage{LoanConfirmation}()
	///		.WithHandler{LoanConfirmationFaultHandler}();
	/// 
	/// public class LoanConfirmationFaultHandler : IFaultHandler{LoanConfirmation}
	/// {
	///		public void HandleFault(IEnvelope envelope, LoanConfirmation message, Exception exception)
	///		{
	///			// do something here in the faulted condition:
	///		}
	/// }
	/// 
	/// </code>
	/// </summary>
	public class FaultHandlerConfiguration
	{
		public FaultHandlerConfiguration()
		{
			FaultHandlers = new HashSet<Type>();
		}

		public Type Message { get; set; }
		public HashSet<Type> FaultHandlers { get; private set; }

		/// <summary>
		/// This will set the message that the specific set of sequential consumers should be configured to. 
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to set the specific handling chain for.</typeparam>
		/// <returns></returns>
		public FaultHandlerConfiguration ForMessage<TMessage>()
			where TMessage : class, IMessage
		{
			Message = typeof (TMessage);
			return this;
		}

		/// <summary>
		/// This will configure a fault handler for a specific message.
		/// </summary>
		/// <typeparam name="TFaultHandler">Type of the fault handler for the message</typeparam>
		/// <returns></returns>
		public FaultHandlerConfiguration WithHandler<TFaultHandler>()
		{
			if (Message == null)
				throw new ArgumentException(
					"The message must first be defined for the fault handler configuration before specifying the fault handlers.");

			Type faultHandlerType = typeof (FaultConsumer<>).MakeGenericType(Message);

			if (faultHandlerType.IsAssignableFrom(typeof (TFaultHandler)))
			{
				FaultHandlers.Add(typeof (TFaultHandler));
			}
			else
			{
				throw new ArgumentException(string.Format("The fault handler '{0}' is not derivable from '{1}.",
				                                          typeof (TFaultHandler).FullName,
				                                          typeof (FaultConsumer<>).MakeGenericType(Message).FullName));
			}

			return this;
		}
	}
}