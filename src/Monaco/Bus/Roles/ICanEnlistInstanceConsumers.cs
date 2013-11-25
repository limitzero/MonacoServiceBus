using Monaco.Bus.Internals;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role to allow message consumers to be temporarily registered for message consumption.
	/// </summary>
	public interface ICanEnlistInstanceConsumers
	{
		/// <summary>
		/// This will add a one-time instance consumer w/subscription 
		/// to the service bus and will be enlisted in the global set 
		/// of consumer to message subscriptions. Also, the consumer 
		/// must inherit from <seealso cref="TransientConsumerOf{T}"/>
		/// to participate in the instance subscription and can be unregistered
		/// by calling the Dispose method on the return token token if needed.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer for transient message consumption.</typeparam>
		/// <returns>
		/// A <seealso cref="IDisposableAction"/> that can be called to remove the temporary registration.
		/// </returns>
		IDisposableAction AddInstanceConsumer<TConsumer>() where TConsumer : IConsumer;

		/// <summary>
		/// This will add a one-time instance consumer w/subscription 
		/// to the service bus and will be enlisted in the global set 
		/// of consumer to message subscriptions. Also, the consumer 
		/// must inherit from <seealso cref="TransientConsumerOf{T}"/>
		/// to participate in the instance subscription and can be unregistered
		/// by calling the Dispose method on the return token if needed.
		/// </summary>
		/// <param name="instance">The current instance of the instance subscription.</param>
		/// <returns>
		/// A <seealso cref="IDisposableAction"/> that can be called to remove the temporary registration.
		/// </returns>
		IDisposableAction AddInstanceConsumer(object instance);
	}
}