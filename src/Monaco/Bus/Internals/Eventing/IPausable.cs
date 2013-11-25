namespace Monaco.Bus.Internals.Eventing
{
	/// <summary>
	/// Contract for any run-time agent that can be paused.
	/// </summary>
	public interface IPausable
	{
		/// <summary>
		/// Gets the flag indicating whether or not the component is paused for processing.
		/// </summary>
		bool IsPaused { get; }

		/// <summary>
		/// This will pause the processing of a component.
		/// </summary>
		void Pause();

		/// <summary>
		/// This will resume the processing for a component.
		/// </summary>
		void Resume();
	}
}