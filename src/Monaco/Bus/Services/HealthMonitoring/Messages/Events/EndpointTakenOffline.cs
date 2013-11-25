using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	public class EndpointTakenOffline : IAdminMessage
	{
		public DateTime At { get; set; }
		public string Duration { get; set; }
		public string Endpoint { get; set; }
	}
}