using System;

namespace Monaco.Bus.MessageManagement.FaultHandling
{
	/// <summary>
	/// Contract for a fault handler that will carry out specific actions for a given message.
	/// </summary>
	/// <typeparam name="TMessage">Concrete type of the message to apply a fault handling condition for.</typeparam>
	public interface FaultConsumer<TMessage> : Consumes<TMessage> where TMessage : IMessage
	{
		/// <summary>
		/// Gets or sets the optional processing envelope message with all run-time details.
		/// </summary>
		IEnvelope Envelope { get; set; }

		/// <summary>
		/// Gets or sets the optional exception that is attached to the fault condition
		/// </summary>
		Exception Exception { get; set; }
	}
}