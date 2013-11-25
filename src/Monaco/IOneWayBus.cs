using System;

namespace Monaco
{
	public interface IOneWayBus
	{
		void Send(Uri endpoint, params IMessage[] messages);
		void Send(Uri endpoint, IMessage message);
		void Send<TMessage>(Uri endpoint, Action<TMessage> action) where TMessage : IMessage;
	}
}