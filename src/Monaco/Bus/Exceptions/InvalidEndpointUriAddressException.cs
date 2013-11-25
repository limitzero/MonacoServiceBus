using System;

namespace Monaco.Bus.Exceptions
{
	public class InvalidEndpointUriAddressException : ApplicationException
	{
		private const string _message =
			"The following uri for the endpoint '{0}' is not in the expected format. The expected format should be '{1}'.";

		public InvalidEndpointUriAddressException(string invalidUri, string expectedUriFormat)
			: base(string.Format(_message, invalidUri, expectedUriFormat))
		{
		}

		public InvalidEndpointUriAddressException(string invalidUri, string expectedUriFormat, Exception inner)
			: base(string.Format(_message, invalidUri, expectedUriFormat), inner)
		{
		}
	}
}