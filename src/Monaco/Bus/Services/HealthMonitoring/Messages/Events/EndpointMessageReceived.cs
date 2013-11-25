using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message to record when an endpoint has received a message from the physical location.
	/// </summary>
	[Serializable]
	public class EndpointMessageReceived : IAdminMessage
	{
		public EndpointMessageReceived()
		{
		}

		public EndpointMessageReceived(string endpointUri, string message)
		{
			EndpointUri = endpointUri;
			Message = message;
		}

		public Guid CorrelationId { get; set; }
		public string EndpointUri { get; set; }
		public string Message { get; set; }
	}
}