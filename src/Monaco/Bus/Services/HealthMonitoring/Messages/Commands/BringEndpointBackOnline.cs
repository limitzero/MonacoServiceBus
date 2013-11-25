using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Commands
{
	/// <summary>
	/// Message to send from control to local bus 
	/// instance to start the processing of messages
	/// from the endpoint location.
	/// </summary>
	public class BringEndpointBackOnline : IAdminMessage
	{
		public string Endpoint { get; set; }
	}
}