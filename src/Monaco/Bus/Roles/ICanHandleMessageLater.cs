using System;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role that allows a bus instance to delay the handling of a message.
	/// </summary>
	public interface ICanHandleMessageLater
	{
		/// <summary>
		/// This will enqueue a message for the bus to handle at a later time.
		/// </summary>
		/// <param name="delay">Amount of time to delay handling of the mesasge</param>
		/// <param name="message">Message to delay delivery</param>
		void Defer(TimeSpan delay, IMessage message);
	}
}