using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Commands
{
	/// <summary>
	/// Message received from a client endpoint for an on-demand response from a "ping" request.
	/// </summary>
	public class Pong : IAdminMessage
	{
		public virtual DateTime ReceivedAt { get; set; }
		public virtual TimeSpan Delta { get; set; }
		public virtual string ResponderEndpoint { get; set; }
		public virtual string RequestorEndpoint { get; set; }
	}
}