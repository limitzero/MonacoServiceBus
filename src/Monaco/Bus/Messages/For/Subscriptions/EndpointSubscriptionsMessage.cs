using System;
using System.Collections.Generic;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus.Messages.For.Subscriptions
{
	[Serializable]
	public class EndpointSubscriptionsMessage : IAdminMessage
	{
		public EndpointSubscriptionsMessage()
		{
		}

		public EndpointSubscriptionsMessage(string endpoint, ICollection<Subscription> subscriptions)
		{
			Endpoint = endpoint;
			Subscriptions = new List<Subscription>(subscriptions);
		}

		public string Endpoint { get; set; }
		public ICollection<Subscription> Subscriptions { get; set; }
	}
}