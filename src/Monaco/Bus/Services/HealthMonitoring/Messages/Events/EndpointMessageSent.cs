using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message to record when an endpoint has sent a message. 
	/// </summary>
	[Serializable]
	public class EndpointMessageSent : IAdminMessage
	{
		public EndpointMessageSent()
		{
		}

		public EndpointMessageSent(string endpointName, string endpointUri, object message)
		{
			EndpointName = endpointName;
			EndpointUri = endpointUri;
			Message = message;
		}

		public Guid CorrelationId { get; set; }
		public string EndpointName { get; set; }
		public string EndpointUri { get; set; }
		public object Message { get; set; }
	}
}