using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Commands
{
	/// <summary>
	/// Message sent by the control endpoint to a client endpoint to denote if the endpoint is avaliable for processing messages.
	/// </summary>
	public class Ping : IAdminMessage
	{
		public virtual DateTime SentAt { get; set; }
		public virtual string RequestorEndpoint { get; set; }
	}
}