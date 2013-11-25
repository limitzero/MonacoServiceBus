using System;

namespace Monaco.Transport
{
	/// <summary>
	/// Concrete instance of message that will be send and/or received from the transport.
	/// </summary>
	[Serializable]
	public class TransportMessage : ITransportMessage
	{
		public TransportMessage()
		{
		}

		public TransportMessage(IMessage message)
			: this(message, null)
		{
		}

		public TransportMessage(byte[] stream)
			: this(null, stream)
		{
		}

		public TransportMessage(IMessage message, byte[] stream)
			: this(message, stream, message.GetType().FullName)
		{
		}

		public TransportMessage(IMessage message, byte[] stream, string label)
		{
			if (message != null)
				Message = message;

			Stream = stream;
			Label = label.Replace("+", ".");
		}

		#region ITransportMessage Members

		public string Endpoint { get; set; }
		public object MessageId { get; set; }
		public string Label { get; set; }
		public IMessage Message { get; set; }
		public byte[] Stream { get; set; }

		public ITransportMessage SetMessage(IMessage message)
		{
			Message = message;
			Label = message.GetType().FullName;
			return this;
		}

		public ITransportMessage SetStream(byte[] stream)
		{
			Stream = stream;
			return this;
		}

		#endregion
	}
}