using Monaco.Bus.Services.Timeout.Messages.Commands;

namespace Monaco.Bus.Services.Timeout
{
	/// <summary>
	/// Agent that runs in the background that can allow 
	/// messages to be delayed for delivery by a certain
	/// interval of time and also persist these instances 
	/// to a datastore.
	/// </summary>
	public interface ITimeoutsService
	{
		/// <summary>
		/// Gets or sets the instance of the service bus;
		/// </summary>
		IServiceBus Bus { get; set; }

		/// <summary>
		/// This will register a scheduled timeout 
		/// for an indicated message to be delivered.
		/// </summary>
		/// <param name="timeout">Message scheduled for timeout.</param>
		void RegisterTimeout(ScheduleTimeout timeout);

		/// <summary>
		/// This will cancel a scheduled timeout 
		/// by the indicated correlation identifier.
		/// </summary>
		/// <param name="timeout">Current timeout to cancel.</param>
		void RegisterCancel(CancelTimeout timeout);
	}
}