using System;

namespace Monaco
{
	public interface IControlBus
	{
		bool IsAvailable { get; }
		void Send(IMessage[] messages);
		void Send(IMessage message);
		void Send<TMessage>(Action<TMessage> action) where TMessage : IMessage;
	}
}