using System;

namespace Monaco.Bus.Exceptions
{
	public class MessageBatchExceededException : ApplicationException
	{
		private const string _message =
			"The current batch of messages submitted and/or the collection on the message exceeds the limit of 256 items. " +
			"Please consider breaking the batch into smaller messages for publication within the infrastructure.";

		public MessageBatchExceededException()
			: base(_message)
		{
		}
	}
}