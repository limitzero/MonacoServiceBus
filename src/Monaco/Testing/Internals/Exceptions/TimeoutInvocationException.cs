using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class TimeoutInvocationException : Exception
	{
		public TimeoutInvocationException(string message)
			: base(message)
		{
		}
	}
}