using System;
using System.Collections.Generic;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus.Messages.For.Subscriptions
{
	public class AvailableSubscriptionsMessage : IMessage
	{
		public Guid CorrelationId { get; set; }
		public ICollection<Subscription> Subscriptions { get; set; }
	}
}