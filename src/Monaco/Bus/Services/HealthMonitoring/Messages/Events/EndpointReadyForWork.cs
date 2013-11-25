using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message that is sent to the control endpoint 
	/// to denote that a local bus instance is up and 
	/// ready to accept messages.
	/// </summary>
	public class EndpointReadyForWork : IAdminMessage
	{
		public string Endpoint { get; set; }
	}
}