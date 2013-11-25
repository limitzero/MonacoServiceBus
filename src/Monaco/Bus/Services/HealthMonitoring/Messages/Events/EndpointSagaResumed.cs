using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Event that denotes when a saga has been resumed after a suspension period on a given endpoint.
	/// </summary>
	public class EndpointSagaResumed : IAdminMessage
	{
		public string Saga { get; set; }
		public string Endpoint { get; set; }
	}
}