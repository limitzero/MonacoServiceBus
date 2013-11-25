using System;
using Monaco.Bus.Services.Timeout.Messages.Commands;

namespace Monaco.Bus.MessageManagement.Callbacks
{
	/// <summary>
	/// This is the contract for the message bus to send messages 
	/// and invoke the caller as to the response for the request.
	/// </summary>
	public interface ICallback
	{
		IServiceAsyncRequest AsyncRequest { get; set; }

		/// <summary>
		/// Sets the response message type to send to client.
		/// </summary>
		object ResponseMessage { get; }

		/// <summary>
		/// Gets or sets the request message that is sent to the 
		/// bus for a corresponding reply message.
		/// </summary>
		object RequestMessage { get; }

		/// <summary>
		/// Gets the callback function to execute on the client.
		/// </summary>
		Action Callback { get; }

		/// <summary>
		/// Registers an action to callback when the message bus sends a reply 
		/// to a message that was sent (synchronous communication).
		/// </summary>
		/// <param name="callback">The callback action to handle the response message</param>
		ICallback Register(Action callback);

		/// <summary>
		/// Registers a timeout on the callback for a send/reply session.
		/// </summary>
		/// <param name="timeout"></param>
		/// <returns></returns>
		ICallback WithTimeout(ScheduleTimeout timeout);

		/// <summary>
		/// This will extract the reply message when the bus has sent a reply 
		/// for a given request on the same logical endpoint.
		/// </summary>
		/// <typeparam name="TReplyMessage">Type of the reply message to extract</typeparam>
		/// <returns></returns>
		TReplyMessage GetReply<TReplyMessage>() where TReplyMessage : IMessage;

		void Complete(object replyMessage);

		void Start(object request);
	}
}