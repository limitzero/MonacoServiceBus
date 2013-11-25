using System;
using System.Collections.Generic;
using Monaco.Bus.Messages;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus.Services.Subscriptions.Messages.Events
{
	public class SubscriptionsRemoved : IAdminMessage
	{
		public DateTime At { get; set; }
		public string Endpoint { get; set; }
		public ICollection<Subscription> Subscriptions { get; set; }
	}
}