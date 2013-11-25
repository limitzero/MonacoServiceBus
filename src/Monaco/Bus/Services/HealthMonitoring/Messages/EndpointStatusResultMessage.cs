using System.Collections.Generic;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages
{
	public class EndpointStatusResultMessage : IAdminMessage
	{
		public EndpointStatusResultMessage()
		{
			if (Statistics == null)
			{
				Statistics = new List<HealthMonitoringStatistic>();
			}
		}

		public List<HealthMonitoringStatistic> Statistics { get; set; }

		public void AddStatistics(params HealthMonitoringStatistic[] statistics)
		{
			Statistics.AddRange(statistics);
		}
	}
}