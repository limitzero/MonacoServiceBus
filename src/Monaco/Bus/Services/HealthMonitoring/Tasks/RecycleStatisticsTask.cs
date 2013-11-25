using Monaco.Bus.Services.HealthMonitoring.Messages.Commands;

namespace Monaco.Bus.Services.HealthMonitoring.Tasks
{
	/// <summary>
	/// Task to periodically force the health service to 
	/// flush the current set of statistics and begin 
	/// a new interval for collection.
	/// </summary>
	public class RecycleStatisticsTask :
		Produces<RecycleStatisticsMessage>
	{
		#region Produces<RecycleStatisticsMessage> Members

		public RecycleStatisticsMessage Produce()
		{
			return new RecycleStatisticsMessage();
		}

		#endregion
	}
}