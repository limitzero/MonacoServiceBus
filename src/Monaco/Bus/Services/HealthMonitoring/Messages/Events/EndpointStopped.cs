using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message that is sent to the control endpoint 
	/// to denote that a local bus instance has been 
	/// shut-down.
	/// </summary>
	public class EndpointStopped : IAdminMessage
	{
		public string Endpoint { get; set; }
	}
}