using Monaco.Bus.Services.HealthMonitoring.Messages.Events;

namespace Monaco.Bus.Services.HealthMonitoring.Tasks
{
	public class EndpointHeartBeatTask :
		Produces<EndpointHeartBeat>
	{
		public EndpointHeartBeatTask()
		{
		}

		public EndpointHeartBeatTask(string endpointName,
		                             string endpointUri,
		                             string endpointHeartbeatInterval,
		                             string endpointHeartbeatGracePeriod)
		{
			EndpointName = endpointName;
			EndpointUri = endpointUri;
			EndpointHeartbeatInterval = endpointHeartbeatInterval;
			EndpointHeartbeatGracePeriod = endpointHeartbeatGracePeriod;
		}

		public string EndpointName { get; set; }
		public string EndpointUri { get; set; }
		public string EndpointHeartbeatInterval { get; set; }
		public string EndpointHeartbeatGracePeriod { get; set; }

		#region Produces<EndpointHeartBeat> Members

		public EndpointHeartBeat Produce()
		{
			return new EndpointHeartBeat(
				EndpointName,
				EndpointUri,
				EndpointHeartbeatInterval,
				EndpointHeartbeatGracePeriod);
		}

		#endregion
	}
}