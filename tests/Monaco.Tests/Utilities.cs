using System;
using System.Collections.Generic;
using System.Linq;

namespace Monaco.Tests
{
	public class Utilities
	{
		static Utilities()
		{
		}

		public static bool IsMessageReceived<TMessage>(IEnumerable<IMessage> messages)
		{
			return IsMessageReceived(messages, typeof (TMessage));
		}

		public static bool IsMessageReceived(IEnumerable<IMessage> messages, Type typeToLookFor)
		{
			return (from message in messages where message.GetType() == typeToLookFor 
					select message).FirstOrDefault() != null;
		}

		public static bool IsMessagePresentInQueue(string queuePath, object message)
		{
			//var path = MsmqTransport.Normalize(queuePath);
			//MessageQueue queue = new MessageQueue(path, QueueAccessMode.ReceiveAndAdmin);
			//var queueMessage = queue.Peek(TimeSpan.FromSeconds(5));
			return false;
		}
	}
}