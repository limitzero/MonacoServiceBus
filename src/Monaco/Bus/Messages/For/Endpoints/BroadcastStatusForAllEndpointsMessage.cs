using System;

namespace Monaco.Bus.Messages.For.Endpoints
{
	public class BroadcastStatusForEndpointMessage : IAdminMessage
	{
		public Guid CorrelationId { get; set; }
	}
}