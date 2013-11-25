using System;
using Monaco.Bus;

namespace Monaco.Transport
{
	/// <summary>
	/// Event arguements for when a message is received from the transport..
	/// </summary>
	[Serializable]
	public class MessageReceivedEventArgs : EventArgs
	{
		public MessageReceivedEventArgs(IEnvelope envelope)
		{
			Envelope = envelope;
		}

		public IEnvelope Envelope { get; private set; }
	}
}