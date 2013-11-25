using System;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	[Serializable]
	public class EndpointHeartBeat : IMessage
	{
		public EndpointHeartBeat()
		{
		}

		public EndpointHeartBeat(string endpointName, string uri, string interval, string period)
		{
			EndpointName = endpointName;
			EndpointUri = uri;
			Interval = interval;
			Period = period;
			At = DateTime.Now;
		}

		public Guid CorrelationId { get; set; }
		public string Interval { get; set; }
		public string Period { get; set; }
		public string GracePeriod { get; set; }
		public string EndpointName { get; set; }
		public string EndpointUri { get; set; }
		public DateTime At { get; set; }
		public DateTime Received { get; set; }
	}
}