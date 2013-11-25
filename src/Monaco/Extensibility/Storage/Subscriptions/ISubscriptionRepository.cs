using System;
using System.Collections.Generic;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Extensibility.Storage.Subscriptions
{
	/// <summary>
	/// Contract for the local subscription repository matching messages to their corresponding components.
	/// </summary>
	public interface ISubscriptionRepository
	{
		/// <summary>
		/// Gets the collection of all locally registered subscriptions.
		/// </summary>
		ICollection<Subscription> Subscriptions { get; }

		/// <summary>
		/// This will search for all components that can 
		/// process the current message within the bus instance.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		ICollection<Subscription> Find(Type message);

		/// <summary>
		/// This will register a unique subscription instance in the repository.
		/// </summary>
		/// <param name="subscription"></param>
		void Register(ISubscription subscription);

		/// <summary>
		/// This will unregister a unique subscription instance in the repository.
		/// </summary>
		/// <param name="subscription"></param>
		void Unregister(ISubscription subscription);
	}
}