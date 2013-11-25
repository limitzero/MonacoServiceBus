using System;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role to allow for publishing messages to all interested subscribers.
	/// </summary>
	public interface ICanPublishMessages
	{
		void Publish<TMessage>() where TMessage : IMessage;

		/// <summary>
		/// This will publish the message to the message consumers.
		/// </summary>
		/// <typeparam name="TMessage">The message to publish out to interested subscribers.</typeparam>
		/// <param name="action">The action to create the message intended for publication.</param>
		/// <returns></returns>
		void Publish<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new();

		/// <summary>
		/// This will publish a  message to the message consumers.
		/// </summary>
		/// <returns></returns>
		void Publish(IMessage message);

		/// <summary>
		/// This will publish the current set of messages to the message consumers.
		/// </summary>
		/// <param name="messages">Array of  messages for publication</param>
		/// <returns></returns>
		void Publish(params object[] messages);

		/// <summary>
		/// This will send out a message to all interested parties and if there is no matching
		///  subscriber to the message, then the message will be disregarded.
		/// </summary>
		/// <param name="messages">Set of messages to send</param>
		/// <returns></returns>
		void Notify(params object[] messages);

		/// <summary>
		/// This will send out a message to all interested parties and if there is no matching
		///  subscriber to the message, then the message will be disregarded.
		/// </summary>
		/// <typeparam name="TMessage">The message for notification to all interested parties.</typeparam>
		/// <param name="action">Delegate to create the message</param>
		/// <returns></returns>
		void Notify<TMessage>(Action<TMessage> action)
			where TMessage : class, IMessage, new();
	}
}