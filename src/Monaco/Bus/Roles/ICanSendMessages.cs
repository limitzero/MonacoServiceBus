using System;
using Monaco.Bus.MessageManagement.Callbacks;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role to allow for sending of messages to the local and remote endpoint.
	/// </summary>
	public interface ICanSendMessages
	{
		ICallback Send<TMessage>() where TMessage : IMessage;

		/// <summary>
		/// This will send the message(s) directly to the message owner.
		/// </summary>
		/// <param name="message">Message(s) to send.</param>
		ICallback Send(params object[] message);

		/// <summary>
		/// This will send a series of messages to a given endpoint.
		/// </summary>
		/// <param name="endpoint">The location to publish the message to.</param>
		/// <param name="messages">The messages intended for publication.</param>
		/// <returns></returns>
		ICallback Send(Uri endpoint, params object[] messages);

		/// <summary>
		/// This will send a singular message and optionally register a callback action 
		/// to be executed when the corresponding reply action is triggered on the 
		/// message consumer.
		/// </summary>
		/// <param name="message">The message to send directly to the message owner</param>
		/// <returns></returns>
		ICallback Send(IMessage message);

		/// <summary>
		/// This will send a singular message and optionally register a callback action 
		/// to be executed when the corresponding reply action is triggered on the 
		/// message consumer.
		/// </summary>
		/// <param name="action">The action to create the message to send directly to the message owner</param>
		/// <returns></returns>
		ICallback Send<TMessage>(Action<TMessage> action) where TMessage : class, IMessage, new();
	}
}