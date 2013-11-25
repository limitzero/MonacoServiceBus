using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class StateMachineStateTransitionInvocationException : Exception
	{
		private const string error_message =
			"The state machine '{0}' was expected to transition to state '{1}' after receiving message '{2}' but failed. Current state '{3}'.";

		public StateMachineStateTransitionInvocationException(Type statemachine,
		                                                      Type message,
		                                                      string expectedState,
		                                                      string currentState)
			: base(string.Format(error_message, statemachine.Name, expectedState, message.Name, currentState))
		{
		}
	}
}