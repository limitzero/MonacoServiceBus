using System.IO;

namespace Monaco.Bus.Internals
{
	/// <summary>
	/// Contract for basic sending and receiving to and from a message store.
	/// </summary>
	public interface IMessageStore
	{
		/// <summary>
		/// This will send a message to the indicated message store.
		/// </summary>
		/// <param name="message">The message to send to storage.</param>
		void Send(Stream message);

		/// <summary>
		/// This will receive a message from the desired message store.
		/// </summary>
		/// <returns>
		/// <code>Stream - if successfull</code>
		/// <code>Null - if unsuccessfull</code>
		/// </returns>
		Stream Receive();
	}
}