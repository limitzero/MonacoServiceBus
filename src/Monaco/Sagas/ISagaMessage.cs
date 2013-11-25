using System;

namespace Monaco.Sagas
{
	/// <summary>
	/// Contract for a message that will be participating in long running processes. All saga messages
	/// must be correlated in order to extract the proper persistant state for the given saga instance.
	/// </summary>
	public interface ISagaMessage : CorrelatedBy<Guid>, IMessage
	{
	}
}