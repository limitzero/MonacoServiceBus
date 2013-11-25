using Monaco.Bus.Services.HealthMonitoring.Messages.Commands;

namespace Monaco.Bus.Services.HealthMonitoring.Tasks
{
	/// <summary>
	/// Task to periodically produce the message to sent the status 
	/// of the endpoint to a remote listner.
	/// </summary>
	public class PrepareEndpointStatusTask :
		Produces<PrepareEndpointStatus>
	{
		#region Produces<PrepareEndpointStatus> Members

		public PrepareEndpointStatus Produce()
		{
			return new PrepareEndpointStatus();
		}

		#endregion
	}
}