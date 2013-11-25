using System;

namespace Monaco.Testing.Internals.Exceptions
{
	public class PublishInvocationException : Exception
	{
		public PublishInvocationException(string message)
			: base(message)
		{
		}
	}
}