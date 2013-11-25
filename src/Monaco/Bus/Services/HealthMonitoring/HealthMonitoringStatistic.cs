using System;
using System.Collections.Generic;
using Monaco.Bus.Services.HealthMonitoring.Messages.Events;
using Monaco.Extensions;

namespace Monaco.Bus.Services.HealthMonitoring
{
	[Serializable]
	public class HealthMonitoringStatistic : IMessage
	{
		public HealthMonitoringStatistic()
		{
		}

		public HealthMonitoringStatistic(string endpointUri)
		{
			EndpointUri = endpointUri;
			Heartbeats = new List<EndpointHeartBeat>();
		}

		public string EndpointUri { get; set; }
		public int NumberOfMessagesSent { get; set; }
		public int NumberOfMessagesReceived { get; set; }
		public List<EndpointHeartBeat> Heartbeats { get; set; }
		public EndpointMetricStatus HeartbeatStatus { get; set; }

		public void RecordHeartBeat(EndpointHeartBeat message)
		{
			Heartbeats.Add(message);
		}

		public void RecordMessageSent()
		{
			NumberOfMessagesSent++;
		}

		public void RecordMessageReceived()
		{
			NumberOfMessagesReceived++;
		}

		public void SetHeartBeatStatus()
		{
			int failureCount = 0;
			int successCount = 0;

			foreach (EndpointHeartBeat heartbeat in Heartbeats)
			{
				// compute the number of times the heartbeat failed to be reported:
				TimeSpan duration = heartbeat.Received - heartbeat.At;
				TimeSpan gracePeriod = new TimeSpan().CreateFromInterval(heartbeat.GracePeriod).Value;

				if (duration.Seconds > gracePeriod.Seconds)
				{
					failureCount++;
				}
				else
				{
					successCount++;
				}
			}

			if (failureCount > successCount)
			{
				HeartbeatStatus = EndpointMetricStatus.Suspect;
			}
			else
			{
				HeartbeatStatus = EndpointMetricStatus.Normal;
			}
		}
	}
}