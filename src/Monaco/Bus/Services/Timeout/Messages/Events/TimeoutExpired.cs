using System;
using Monaco.Bus.Messages;

namespace Monaco.Bus.Services.Timeout.Messages.Events
{
	public class TimeoutExpired : IAdminMessage, CorrelatedBy<Guid>
	{
		/// <summary>
		/// Gets or sets the bus endpoint that generated the timeout message.
		/// </summary>
		public string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the message that is to be delivered after the timeout period.
		/// </summary>
		public object Message { get; set; }

		/// <summary>
		/// Gets the date and time that the timeout was requested.
		/// </summary>
		public DateTime RequestedAt { get; set; }

		#region CorrelatedBy<Guid> Members

		public Guid CorrelationId { get; set; }

		#endregion

		public override string ToString()
		{
			string msg = string.Format("Timeout expired for message '{0}' with instance identifier of '{1}' " +
			                           "requested at '{2}' delivered at '{3}' on endpoint '{4}'.",
			                           Message.GetType().FullName,
			                           CorrelationId,
			                           RequestedAt,
			                           DateTime.Now,
			                           Endpoint);

			return msg;
		}
	}
}