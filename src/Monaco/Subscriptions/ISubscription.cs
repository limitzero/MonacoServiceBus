using System;

namespace Monaco.Subscriptions
{
	/// <summary>
	/// Semantic matching the endpoint, consumer and message together for publish/subscribe.
	/// </summary>
	public interface ISubscription : IMessage
	{
		/// <summary>
		/// Gets or sets the unique identifier for the subscription.
		/// </summary>
		Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the flag indicating whether or not the subscription is active (false = 
		/// messages will not be routed to the component for processing but will be pushed
		/// to alternate queue for re-subscription).
		/// </summary>
		bool IsActive { get; set; }

		/// <summary>
		/// Gets or sets the uri address where the component is located. 
		/// </summary>
		string Uri { get; set; }

		/// <summary>
		/// Gets or sets the fully qualified name of the component that can process the message.
		/// </summary>
		string Component { get; set; }

		/// <summary>
		/// Gets or sets the fully qualified name of the message that the component can process.
		/// </summary>
		string Message { get; set; }
	}
}