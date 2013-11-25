using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message that is sent to the control endpoint 
	/// to denote that a local bus instance has been 
	/// started.
	/// </summary>
	public class EndpointStarted : IAdminMessage
	{
		public string Endpoint { get; set; }
	}
}