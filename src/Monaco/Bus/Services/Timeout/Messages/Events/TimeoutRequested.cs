using Monaco.Bus.Messages;
using Monaco.Bus.Services.Timeout.Messages.Commands;

namespace Monaco.Bus.Services.Timeout.Messages.Events
{
	/// <summary>
	/// Message sent to the control endpoint to denoted that a timeout 
	/// has been scheduled for a message on a particular endpoint.
	/// </summary>
	public class TimeoutRequested : IAdminMessage
	{
		public ScheduleTimeout ScheduledTimeout { get; set; }
	}
}