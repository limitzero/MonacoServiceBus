using Monaco.Bus.Messages;
using Monaco.Bus.Services.Timeout.Messages.Commands;

namespace Monaco.Bus.Services.Timeout.Messages.Events
{
	/// <summary>
	/// Message sent to the control endpoint to denoted that a previously 
	/// scheduled timeout for a message on a particular endpoint has been 
	/// recinded by the infrastructure or user-code.
	/// </summary>
	public class TimeoutCancelled : IAdminMessage
	{
		public CancelTimeout CancelledTimeout { get; set; }
	}
}