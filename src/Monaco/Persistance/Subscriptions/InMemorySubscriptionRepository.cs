using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals.Collections;
using Monaco.Persistance.Repositories;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Persistance.Subscriptions
{
    /// <summary>
    /// Volatile instance of local subscription repository for the local message bus.
    /// </summary>
    public class InMemorySubscriptionRepository : ISubscriptionRepository
    {
        private static readonly object _subscriptions_lock = new object();
        private readonly IThreadSafeList<Subscription> _subscriptions;

        public ICollection<Subscription> Subscriptions
        {
            get { return new List<Subscription>(_subscriptions); }
        }

        public InMemorySubscriptionRepository()
        {
            if(this._subscriptions == null)
            {
                this._subscriptions = new ThreadSafeList<Subscription>();
            }
        }

        public ICollection<Subscription> Find(Type message)
        {
            lock (_subscriptions_lock)
            {
                ICollection<Subscription> subscriptions = (from item in this._subscriptions
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
            	var aSubscription = this._subscriptions.Instance.Where(x => x.Message == subscription.Message && 
					x.Uri == subscription.Uri).FirstOrDefault();

				if(aSubscription == null)
				{
					this._subscriptions.Add(subscription as Subscription);
				}
            }
        }

        public void Unregister(ISubscription subscription)
        {
            lock (_subscriptions_lock)
            {
				var aSubscription = this._subscriptions.Instance.Where(x => x.Message == subscription.Message).FirstOrDefault();

				if (aSubscription != null)
				{
					this._subscriptions.Remove(aSubscription);
				}
            }
        }
    }
}