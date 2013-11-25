namespace Monaco.Bus.MessageManagement.FaultHandling
{
	/// <summary>
	/// Contract for the canonical format of a fault message that will be handled by <seealso cref="FaultConsumer{TMessage}"/>
	/// </summary>
	public interface IFaultMessage<TMessage> : IMessage
	{
		/// <summary>
		/// Gets or sets location where the fault occurred.
		/// </summary>
		string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the message that caused the fault.
		/// </summary>
		TMessage Message { get; set; }

		/// <summary>
		/// Gets or sets the full error exception details for the current fault.
		/// </summary>
		string Exception { get; set; }
	}
}