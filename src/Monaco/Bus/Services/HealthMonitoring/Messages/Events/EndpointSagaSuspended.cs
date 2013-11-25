using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Event that denotes when a saga has been suspended on a given endpoint.
	/// </summary>
	public class EndpointSagaSuspended : IAdminMessage
	{
		public string Saga { get; set; }
		public string Endpoint { get; set; }
		public string Duration { get; set; }
	}
}