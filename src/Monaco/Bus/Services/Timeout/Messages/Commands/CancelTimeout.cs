using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.Timeout.Messages.Commands
{
	[Serializable]
	public class CancelTimeout : IAdminMessage
	{
		public CancelTimeout()
		{
		}

		public CancelTimeout(Guid timeoutId)
		{
			TimeoutId = timeoutId;
		}

		/// <summary>
		/// Gets or sets the instance of the identifier for the timeout message
		/// that will be remove from the instance of registered timeouts.
		/// </summary>
		public Guid TimeoutId { get; set; }
	}
}