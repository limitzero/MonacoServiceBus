using System;

namespace Monaco.Bus.MessageManagement.FaultHandling
{
	/// <summary>
	/// Contract for the fault processor that will look up all fault handlers for a given message and 
	/// execute them according to their configured sequence.
	/// </summary>
	public interface IFaultProcessor
	{
		/// <summary>
		/// This will process the current message and/or exception through the configured fault handlers.
		/// </summary>
		/// <typeparam name="TMessage">Concrete type of message that generated the fault</typeparam>
		/// <param name="message">Message that generated the fault</param>
		/// <param name="envelope">Messageing envelope that contains the full detals during processing.</param>
		/// <param name="exception">Exception (if any) that details the reason for the fault.</param>
		void Process<TMessage>(TMessage message, IEnvelope envelope = null, Exception exception = null);
	}
}