using System;

namespace Monaco.Bus.Internals
{
	/// <summary>
	/// Contract for all internal components that will process a message 
	/// and send the resultant message back for inspection or processing.
	/// </summary>
	public interface IMessageReceiver
	{
		/// <summary>
		/// Gets or sets the inline action that will be preformed when the message is received.
		/// </summary>
		Action<IMessage> OnMessageReceived { get; set; }
	}
}