namespace Monaco.Transport
{
	/// <summary>
	/// Contract for message that will be send and/or received from the transport.
	/// </summary>
	public interface ITransportMessage
	{
		/// <summary>
		/// Gets or sets the endpoint location where the message has been received.
		/// </summary>
		string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the instance of the unique message on the endpoint.
		/// </summary>
		object MessageId { get; set; }

		/// <summary>
		/// Gets or sets the current message that will be sent orreceived via the transport.
		/// </summary>
		IMessage Message { get; }

		/// <summary>
		/// Gets or sets the stream that will be sent orreceived via the transport.
		/// </summary>
		byte[] Stream { get; }

		string Label { get; set; }

		ITransportMessage SetMessage(IMessage message);

		ITransportMessage SetStream(byte[] message);
	}
}