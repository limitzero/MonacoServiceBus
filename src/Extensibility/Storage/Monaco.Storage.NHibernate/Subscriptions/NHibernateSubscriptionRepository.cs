using System;
using System.Collections.Generic;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;
using NHibernate;
using NHibernate.Criterion;

namespace Monaco.Storage.NHibernate.Subscriptions
{
    public class NHibernateSubscriptionRepository : ISubscriptionRepository
    {
        private readonly ISession session;

        public NHibernateSubscriptionRepository(ISession session)
        {
            this.session = session;
        }

        public ICollection<Subscription> Subscriptions
        {
            get
            {
                IList<Subscription> subscriptions = null;

            	DetachedCriteria criteria = DetachedCriteria.For<Subscription>();
            	subscriptions = criteria.GetExecutableCriteria(this.session).List<Subscription>();

            	return subscriptions;
            }
        }

        public ICollection<Subscription> Find(Type message)
        {
            DetachedCriteria criteria = DetachedCriteria.For<Subscription>()
                .Add(Expression.Eq("Message", message.FullName.Trim()));

            var subscriptions = criteria.GetExecutableCriteria(this.session).List<Subscription>();

            return subscriptions;
        }

        public void Register(ISubscription subscription)
        {
            using(var txn = this.session.BeginTransaction())
            {
                try
                {
                    session.Save(subscription);
                    txn.Commit();
                }
                catch
                {
                    txn.Rollback();                    
                    throw;
                }
            }
        }

        public void Unregister(ISubscription subscription)
        {
            using (var txn = session.BeginTransaction())
            {
                try
                {
                    session.Delete(subscription);
                    txn.Commit();
                }
                catch
                {
                    txn.Rollback();
                }
            }
        }

        private static ICollection<ISubscription> ToInterfaceBasedCollection(IEnumerable<Subscription> subscriptions)
        {
            List<ISubscription> theSubscriptions = new List<ISubscription>();

            if (subscriptions != null)
            {
                foreach (var subscription in subscriptions)
                {
                    theSubscriptions.Add(subscription as ISubscription);
                }
            }

            return theSubscriptions;
        }
    }
}