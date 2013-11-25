using System;
using System.Collections.Generic;
using Monaco.StateMachine.Internals.Impl;

namespace Monaco.StateMachine.Internals
{
	public interface IMessageActionRecorder
	{
		ICollection<MessageAction> MessageActions { get; }
		void Play(IMessage message);

		void RecordDoAction<TMessage>(Action<TMessage> action, string notes)
			where TMessage : class, IMessage;

		void RecordPublishAction<TMessage, TMessageToPublish>(Action<TMessage, TMessageToPublish> action)
			where TMessage : class, IMessage
			where TMessageToPublish : IMessage;

		void RecordSendAction<TMessage, TMessageToSend>(Action<TMessage, TMessageToSend> action)
			where TMessage : class, IMessage
			where TMessageToSend : IMessage;

		void RecordSendToEndpointAction<TMessage, TMessageToSend>(Action<TMessage, TMessageToSend> action,
		                                                          Uri endpoint)
			where TMessage : class, IMessage
			where TMessageToSend : IMessage;

		void RecordReplyAction<TMessage, TMessageToReplyWith>(Action<TMessage, TMessageToReplyWith> action)
			where TMessage : class, IMessage
			where TMessageToReplyWith : IMessage;

		void RecordDelayAction<TMessage, TMessageToDefer>(Action<TMessage, TMessageToDefer> action,
		                                                  TimeSpan duration)
			where TMessage : class, IMessage
			where TMessageToDefer : IMessage;

		void RecordTransitionAction<TMessage>(State newState)
			where TMessage : class, IMessage;

		void RecordCompleteAction<TMessage>()
			where TMessage : class, IMessage;

		void RecordConsumeAction<TMessage>(TMessage message)
			where TMessage : class, IMessage;

		void RecordCorrelateAction<TMessage>(Func<TMessage, bool> correlate)
			where TMessage : class, IMessage;

		void RecordPublishActionByType<TSagaMessage, TMessage>(Action<TSagaMessage, TMessage> publish)
			where TMessage : IMessage
			where TSagaMessage : IMessage;
	}
}