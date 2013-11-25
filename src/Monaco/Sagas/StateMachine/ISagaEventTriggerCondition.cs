using System.Collections.Generic;

namespace Monaco.Sagas.StateMachine
{
	public interface ISagaEventTriggerCondition
	{
		/// <summary>
		/// Gets the current message that the trigger conditions will be executed for
		/// </summary>
		IMessage Message { get; }

		/// <summary>
		/// Gets or sets the state that the saga instance has transitioned into.
		/// </summary>
		State State { get; set; }

		/// <summary>
		/// Gets or sets the current event that triggered the message processing.
		/// </summary>
		string Event { get; }

		IMessageActionRecorder Recorder { get; }

		/// <summary>
		/// Gets the set of message actions that will be taken by the state machine.
		/// </summary>
		ICollection<MessageAction> MessageActions { get; }
	}
}