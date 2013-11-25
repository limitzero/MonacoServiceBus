using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class SendInvocationException : Exception
	{
		public SendInvocationException(string message)
			: base(message)
		{
		}
	}
}