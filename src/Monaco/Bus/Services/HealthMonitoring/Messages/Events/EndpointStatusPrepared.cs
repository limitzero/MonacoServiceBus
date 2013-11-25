using System;
using System.Collections.Generic;
using Monaco.Bus.Messages;
using Monaco.Bus.Services.HealthMonitoring.Messages.Commands;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Events
{
	/// <summary>
	/// Message response to the <seealso cref="PrepareEndpointStatus"/> request to 
	/// return the status of the current endpoint.
	/// </summary>
	[Serializable]
	public class EndpointStatusPrepared : IAdminMessage
	{
		public EndpointStatusPrepared()
		{
			if (Statistics == null)
			{
				Statistics = new List<HealthMonitoringStatistic>();
			}
		}

		public List<HealthMonitoringStatistic> Statistics { get; set; }
		public string Endpoint { get; set; }

		public void AddStatistics(params HealthMonitoringStatistic[] statistics)
		{
			Statistics.AddRange(statistics);
		}
	}
}