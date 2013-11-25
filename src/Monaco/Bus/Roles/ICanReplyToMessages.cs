using System;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role to allow replies to be delivered back to the message originator.
	/// </summary>
	public interface ICanReplyToMessages
	{
		/// <summary>
		/// This will send a response to the caller for the request made 
		/// by the <seealso cref="ICanSendMessages.Send(IMessage)"/> method on the message bus.
		/// </summary>
		/// <param name="message">Message to reply with to caller in the message bus</param>
		void Reply(object message);

		/// <summary>
		/// This will send a response to the caller for the request made 
		/// by the <seealso cref="ICanSendMessages.Send(IMessage)"/> method on the message bus.
		/// </summary>
		/// <param name="action">Message to reply with to caller in the message bus</param>
		void Reply<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new();
	}
}