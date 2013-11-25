using System.Collections.Generic;
using Monaco.Bus.Internals;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus
{
	public class CurrentMessageInformation
	{
		public IMessage Message { get; set; }
		public ICollection<Subscription> Subscriptions { get; set; }
		public IConsumer Handler { get; set; }
		public string OriginatorEndpoint { get; set; }
		public IMessage Request { get; set; }
	}
}