using System;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus.Messages.For.Subscriptions
{
	/// <summary>
	/// Message that denotes when a subscription should be 
	/// removed from the local subscription repository as a result 
	/// of global updates from the subscription manager.
	/// </summary>
	[Serializable]
	public class UnregisterSubscriptionMessage : IAdminMessage
	{
		public Subscription Subscription { get; set; }
	}
}