using System;

namespace Monaco.Bus.Exceptions
{
	public class MessageBatchExceededForMessageException : ApplicationException
	{
		private const string _message =
			"The collection on the message '{0}' exceeds the limit of 256 items. " +
			"Please consider breaking the message collection into smaller messages for publication within the infrastructure.";

		public MessageBatchExceededForMessageException(object message)
			: base(string.Format(_message, message.GetType().FullName))
		{
		}
	}
}