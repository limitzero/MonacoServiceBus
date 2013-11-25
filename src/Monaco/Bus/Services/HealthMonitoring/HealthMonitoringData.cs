using System;
using System.Collections.Generic;

namespace Monaco.Bus.Services.HealthMonitoring
{
	[Serializable]
	public class HealthMonitoringData
	{
		public HealthMonitoringData()
		{
			Statistics = new List<HealthMonitoringStatistic>();
		}

		public Guid CorrelationId { get; set; }
		public List<HealthMonitoringStatistic> Statistics { get; set; }
		public string State { get; set; }
		public string Version { get; set; }

		public void RegisterStatistic(HealthMonitoringStatistic statistic)
		{
			if (!Statistics.Contains(statistic))
			{
				Statistics.Add(statistic);
			}
		}
	}
}