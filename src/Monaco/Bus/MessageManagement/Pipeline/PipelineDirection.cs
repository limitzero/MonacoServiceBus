namespace Monaco.Bus.MessageManagement.Pipeline
{
	/// <summary>
	/// This will determine the direction of message passing inside of the service bus 
	/// to the endpoint  (send) and receipt from the endpoint for consumption (receive).
	/// </summary>
	public enum PipelineDirection
	{
		/// <summary>
		/// The send direction of the pipeline works to deliver the message to the endpoint 
		/// of choice. All failed send messages will be deverted to the "error" endpoint for 
		/// inspection and/or recovery.
		/// </summary>
		Send,

		/// <summary>
		/// The receive direction of the pipeline must work in a single threaded fashion 
		/// in order to keep the infrastructure from trying to process the same message 
		/// twice since the defined message endpoint transport can operate in a multi-threaded 
		/// fashion.
		/// </summary>
		Receive,
	}
}