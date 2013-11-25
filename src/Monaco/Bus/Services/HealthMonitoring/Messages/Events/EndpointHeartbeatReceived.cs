using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message sent to the control endpoint to denoted that an 
	/// endpoint has successfully processed the localized 
	/// "heartbeat" message on the remote endpoint.
	/// </summary>
	public class EndpointHeartbeatReceived : IAdminMessage
	{
		public EndpointHeartBeat Heartbeat { get; set; }
	}
}