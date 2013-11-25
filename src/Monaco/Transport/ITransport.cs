using System;
using System.Transactions;
using Monaco.Bus;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Endpoint;

namespace Monaco.Transport
{
	/// <summary>
	/// Contract for all messaging transports for receipt and delivery of messages.
	/// </summary>
	public interface ITransport
	{
		/// <summary>
		/// Gets or sets the transaction isolation level for interactions with transactional resources.
		/// </summary>
		IsolationLevel TransactionIsolationLevel { get; set; }

		/// <summary>
		/// Gets or sets the serialization provider for deserializing messages from the transport (if needed)
		/// </summary>
		ISerializationProvider SerializationProvider { get; set; }

		/// <summary>
		/// Gets or sets the current uri for the endpoint.
		/// </summary>
		IEndpoint Endpoint { get; }

		/// <summary>
		/// Gets or sets the number of concurrent threads used for retrieving messages on the transport.
		/// </summary>
		int NumberOfWorkerThreads { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating whether or not the transport supports transactions.
		/// </summary>
		bool IsTransactional { get; set; }

		/// <summary>
		/// Gets or sets the number of retries that the transport will attempt 
		/// for sending a message to a location.
		/// </summary>
		int MaxRetries { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating whether or not the existing messages in the physical
		/// location will be kept on restarts of the endpoint (true = keep messages after restart; 
		/// false = purge messages on restart {default} ).
		/// </summary>
		bool IsRecoverable { get; set; }

		/// <summary>
		/// Event that is called when a message is starting to be received from a transport.
		/// </summary>
		event Action<ITransport> OnReceiveStart;

		/// <summary>
		/// Event that is called when a message is completed the retrieval from a transport.
		/// </summary>
		event Action<IEnvelope, ITransport> OnReceiveCompleted;

		/// <summary>
		/// Event that is called when a message is received from a transport and ready to be used by the bus.
		/// </summary>
		event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

		/// <summary>
		/// Event that is fired when the receive action can not be completed.
		/// </summary>
		event Action<IEnvelope, Exception> OnReceiveError;

		/// <summary>
		/// Event that is fired when the retry period for the message has been exhausted.
		/// </summary>
		event Action<IEnvelope, Exception> OnRetryExhausted;

		/// <summary>
		/// This will instruct the transport to connect to the 
		/// physical location in order to receive/send messages
		/// </summary>
		void Connect();

		/// <summary>
		/// This will instruct the transport to disconnect from the 
		/// physical location in order to receive/send messages
		/// </summary>
		void Disconnect();

		/// <summary>
		/// This will recycle the current connection to the transport 
		/// for receiving or sending messages.
		/// </summary>
		void Reconnect();

		/// <summary>
		/// This will receive the message within a defined interval via the transport.
		/// </summary>
		/// <param name="timeout">Interval in which to receive the message</param>
		/// <returns>
		///   An implementation of <seealso cref="ITransportMessage"/>  with the 
		///   stream property populated with the current message from the transport
		///   technology endpoint.
		/// </returns>
		IEnvelope Receive(TimeSpan timeout);

		/// <summary>
		/// This will allow the transport to send a message with additional 
		/// metadata attached for fully describing the annoymous stream.
		/// </summary>
		/// <param name="envelope">The transport message to be placed on the transport for delivery.</param>
		void Send(IEnvelope envelope);

		/// <summary>
		/// This will allow the transport to send a message to a desired location
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="envelope">The transport message to be placed on the transport for delivery.</param>
		void Send(IEndpoint endpoint, IEnvelope envelope);

		/// <summary>
		/// This will remove a message on the endpoint by the indentifier.
		/// </summary>
		/// <param name="messageId"></param>
		void RemoveCurrentMessage();
	}
}