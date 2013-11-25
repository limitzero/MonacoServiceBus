using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class StateMachineDataExpectationException : Exception
	{
		public StateMachineDataExpectationException(string message)
			: base(message)
		{
		}
	}
}