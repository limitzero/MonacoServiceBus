using System.Collections.Generic;

namespace Monaco.Testing
{
	public interface IMockServiceBus : IServiceBus
	{
		List<MessageStore> Messages { get; set; }
		void AssertThatMessageWasPublished(IMessage message);
		void AssertThatMessageWasSent(IMessage message);
		void AssertThatMessageWasRepliedTo(IMessage message);
		void Verify();
	}
}