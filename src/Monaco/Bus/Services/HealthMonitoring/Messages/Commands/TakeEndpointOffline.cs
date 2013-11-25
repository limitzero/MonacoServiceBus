using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.HealthMonitoring.Messages.Commands
{
	/// <summary>
	/// Message sent from control to local bus endpoint 
	/// to indicate that the message processing should 
	/// cease on the endpoint for a given duration.
	/// </summary>
	public class TakeEndpointOffline : IAdminMessage
	{
		/// <summary>
		/// Gets or sets the endpoint to halt message processing.
		/// </summary>
		public string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the current time that the command was issued
		/// </summary>
		public DateTime At { get; set; }

		/// <summary>
		/// Gets or sets the duration (hh:mm:ss) in which 
		/// to stop message processing on the endpoint.
		/// </summary>
		public string Duration { get; set; }
	}
}