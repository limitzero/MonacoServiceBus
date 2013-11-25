using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Commands
{
	/// <summary>
	/// Command message sent to force the health service to 
	/// recycle the current accumulated statistics and begin 
	/// the collection period again.
	/// </summary>
	public class RecycleStatisticsMessage : IAdminMessage
	{
	}
}