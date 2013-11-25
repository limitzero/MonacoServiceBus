using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals.Collections;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Extensibility.Storage.Impl.Volatile
{
	/// <summary>
	/// Volatile instance of local subscription repository for the local message bus.
	/// </summary>
	public class InMemorySubscriptionRepository : ISubscriptionRepository
	{
		private static readonly object _subscriptions_lock = new object();
		private readonly IThreadSafeList<Subscription> _subscriptions;

		public InMemorySubscriptionRepository()
		{
			if (_subscriptions == null)
			{
				_subscriptions = new ThreadSafeList<Subscription>();
			}
		}

		public ICollection<Subscription> Subscriptions
		{
			get { return new List<Subscription>(_subscriptions); }
		}

		public ICollection<Subscription> Find(Type message)
		{
			lock (_subscriptions_lock)
			{
				ICollection<Subscription> subscriptions = (from item in _subscriptions
				                                           where
				                                           	item.Message.Trim().ToLower() ==
				                                           	message.FullName.Trim().ToLower()
				                                           select item).ToList();

				return subscriptions;
			}
		}

		public void Register(ISubscription subscription)
		{
			lock (_subscriptions_lock)
			{
				Subscription aSubscription = _subscriptions.Instance.Where(x => x.Message == subscription.Message &&
				                                                                x.Uri == subscription.Uri).FirstOrDefault();

				if (aSubscription == null)
				{
					_subscriptions.Add(subscription as Subscription);
				}
			}
		}

		public void Unregister(ISubscription subscription)
		{
			lock (_subscriptions_lock)
			{
				Subscription aSubscription = _subscriptions.Instance.Where(x => x.Message == subscription.Message).FirstOrDefault();

				if (aSubscription != null)
				{
					_subscriptions.Remove(aSubscription);
				}
			}
		}
	}
}