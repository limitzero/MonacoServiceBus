using System;

namespace Monaco.Bus.Messages.For.Endpoints
{
	/// <summary>
	/// Message to inquire about the status of an endpoint on the message bus.
	/// </summary>
	[Serializable]
	public class EndpointsStatusInquiryMessage : IAdminMessage
	{
		public Guid CorrelationId { get; set; }
		public string Originator { get; set; }
		public string EndpointUri { get; set; }
	}
}