using System;

namespace Monaco.Bus.Messages.For.Recovery
{
	[Serializable]
	public class RecoveryMessage : IAdminMessage
	{
		/// <summary>
		/// Gets or sets the full enveloped message that generated the exception condition for recovery.
		/// </summary>
		public IEnvelope Envelope { get; set; }

		/// <summary>
		/// Gets or sets the date that the exception condition happened.
		/// </summary>
		public DateTime? OccuredAt { get; set; }
	}
}