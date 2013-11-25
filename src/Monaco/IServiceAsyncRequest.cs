using System;
using Monaco.Bus.Internals;

namespace Monaco
{
	/// <summary>
	/// Contract for the exposed contract for a sending a message on the bus asynchronously with a callback.
	/// </summary>
	public interface IServiceAsyncRequest : IAsyncResult
	{
		/// <summary>
		/// Gets the message currently enqueued for an asynchronous response.
		/// </summary>
		IMessage Request { get; }

		/// <summary>
		/// Gets the optionally returned response for the request as a result of the bus sending a reply.
		/// </summary>
		object Response { get; }

		/// <summary>
		///  This will send the message to the indicated message consumer for processing.
		/// </summary>
		/// <param name="message">Message to send to the consumer(s).</param>
		/// <param name="timeout">Optional timespan to wait before checking on the reply message.</param>
		void Send(IMessage message, TimeSpan? timeout = null);

		/// <summary>
		/// This will perform a one-time registration of the service in the container for the bus
		/// to deliver the message to and optionally receive a response.
		/// </summary>
		/// <param name="service"></param>
		/// <returns></returns>
		IServiceAsyncRequest For(object service);

		/// <summary>
		/// This will perform a one-time registration of the service in the container for the bus
		/// to deliver the message to and optionally receive a response.
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		IServiceAsyncRequest For<TService>() where TService : class, IConsumer;

		/// <summary>
		/// This will initialize the async callback structure with the state and current callback.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		IServiceAsyncRequest WithCallback(AsyncCallback callback, object state);

		/// <summary>
		/// This will initialize the async callback structure with the state and current callback.
		/// </summary>
		/// <param name="callback">Custom action to be executed after the request has been completed.</param>
		/// <returns></returns>
		IServiceAsyncRequest WithCallback(Action callback);

		/// <summary>
		/// This will initialize a timeout message to be delivered in the event that the 
		/// operation exceeds a given duration.
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="timeoutMessage"></param>
		/// <returns></returns>
		IServiceAsyncRequest WithTimeout(TimeSpan timeout, IMessage timeoutMessage);

		/// <summary>
		/// This will initialize a timeout message to be delivered in the event that the 
		/// operation exceeds a given duration.
		/// </summary>
		/// <param name="timeout"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		IServiceAsyncRequest WithTimeout<TMessage>(TimeSpan timeout, Action<TMessage> action)
			where TMessage : IMessage;

		/// <summary>
		/// This will be used to terminate the asynchronous request (if needed).
		/// </summary>
		/// <returns></returns>
		void Complete();

		/// <summary>
		/// This will be used to terminate the asynchronous request (if needed).
		/// </summary>
		/// <returns></returns>
		void Complete(object message);

		/// <summary>
		/// This will be used to get a response message (if needed).
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <returns></returns>
		TMessage GetReply<TMessage>() where TMessage : IMessage;
	}
}