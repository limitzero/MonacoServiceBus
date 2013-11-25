using System;
using Monaco.Bus.Services.Subscriptions.Messages.Commands;

namespace Monaco.Bus.Services.Subscriptions.Tasks
{
	/// <summary>
	/// Task to periodically send out the list of subscriptions for this endpoint.
	/// </summary>
	public class PrepareSubscriptionsTask : Produces<PrepareSubscriptions>
	{
		#region Produces<PrepareSubscriptions> Members

		public PrepareSubscriptions Produce()
		{
			return new PrepareSubscriptions {At = DateTime.Now};
		}

		#endregion
	}
}