using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Monaco.StateMachine.Internals.Impl;

namespace Monaco.StateMachine.Internals
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

		/// <summary>
		/// Gets or sets a pre-condition expression to be evaluated before the action is 
		/// taken on the message
		/// </summary>
		Expression<Func<bool>> PreCondition { get; set; }
	}
}