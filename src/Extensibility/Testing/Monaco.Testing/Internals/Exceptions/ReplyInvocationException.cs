using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class ReplyInvocationException : Exception
	{
		public ReplyInvocationException(string message)
			: base(message)
		{
		}
	}
}