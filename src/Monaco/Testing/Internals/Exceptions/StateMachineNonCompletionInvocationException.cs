using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class StateMachineNonCompletionInvocationException : Exception
	{
		private const string error_message =
			"The state machine '{0}' was expected not to complete after receiving message '{1}' but failed to due so.";

		public StateMachineNonCompletionInvocationException(Type statemachine, Type message)
			: base(string.Format(error_message, statemachine.Name, message.Name))
		{
		}
	}
}