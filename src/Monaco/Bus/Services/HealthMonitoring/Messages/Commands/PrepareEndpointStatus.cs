using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Commands
{
	/// <summary>
	/// Message to inquire about the status of a particular endpoint.
	/// </summary>
	[Serializable]
	public class PrepareEndpointStatus : IAdminMessage
	{
		public PrepareEndpointStatus()
		{
		}

		public PrepareEndpointStatus(string endpointUri)
		{
			EndpointUri = endpointUri;
		}

		public string EndpointUri { get; set; }
	}
}