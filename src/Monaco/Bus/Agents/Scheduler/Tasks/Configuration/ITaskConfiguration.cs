using System;

namespace Monaco.Bus.Agents.Scheduler.Tasks.Configuration
{
	/// <summary>
	/// Contract for registering a component task for message production.
	/// </summary>
	public interface ITaskConfiguration
	{
		/// <summary>
		/// Gets or sets the name fo the task.
		/// </summary>
		string TaskName { get; set; }

		/// <summary>
		/// Gets or sets the component type that will be a scheduled task.
		/// </summary>
		Type Component { get; set; }

		/// <summary>
		/// Gets or sets the current instance of the component that will be a scheduled task.
		/// </summary>
		object ComponentInstance { get; set; }

		/// <summary>
		/// Gets or sets the name of the method to call. If the component 
		/// is derived from <seealso cref="Produces{TMessage}"/>, then 
		/// this is configured automatically. An exception is thrown if 
		/// this is left blank and the component does not implement 
		/// the <seealso cref="Produces{TMessage}"/> contract.
		/// </summary>
		string MethodName { get; set; }

		/// <summary>
		/// Gets or sets the interval {hh:mm:ss} that the task should be invoked.
		/// </summary>
		string Interval { get; set; }

		/// <summary>
		/// Gets or sets the flag to indicate if the task has an error, should it be forcibly stopped.
		/// </summary>
		bool HaltOnError { get; set; }

		/// <summary>
		/// Gets or sets the flag to indicate that the task should immediately be invoked at the time that the bus is started
		/// and after that, resume its normal schedule of invocation.
		/// </summary>
		bool ForceStart { get; set; }
	}
}