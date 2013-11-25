using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Reflection.Impl;

namespace Monaco.StateMachine.Internals.Impl
{
	public class MessageActionRecorder : IMessageActionRecorder
	{
		private readonly List<MessageAction> _messageActions;
		private readonly SagaStateMachine _stateMachine;
		private bool _isInMockedState;

		public MessageActionRecorder(SagaStateMachine stateMachine)
		{
			_stateMachine = stateMachine;
			_messageActions = new List<MessageAction>();
			_isInMockedState = true;

			// if in mocked state, do not execute the bus actions for transmitting message:
			_isInMockedState = _stateMachine.Bus.GetType().FullName.ToLower().Trim().Contains("proxy");
		}

		public ICollection<MessageAction> MessageActions
		{
			get { return _messageActions; }
		}

		#region IMessageActionRecorder Members

		public void Play(IMessage message)
		{
			// Transistion, When and Correlate are performed by the dispatcher:
			List<MessageAction> actions = (from match in MessageActions
			                               where match.ActionType != SagaStateMachineMessageActionType.Transition
			                                     && match.ActionType != SagaStateMachineMessageActionType.When
			                                     && match.ActionType != SagaStateMachineMessageActionType.Correlate
			                               select match).Distinct().ToList();

			// make the recorder playback in non-mocked mode:
			_isInMockedState = false;

			// execute the actions on the state machine as recorded:
			actions.ForEach(x => x.Action(message));
		}

		public void RecordConsumeAction<TMessage>(TMessage message)
			where TMessage : class, IMessage
		{
			Action<IMessage> consumeAction = (sagaMessage) =>
			                                 	{
			                                 		((Consumes<TMessage>) _stateMachine)
			                                 			.Consume((TMessage) sagaMessage);
			                                 	};

			var messageAction = new MessageAction(SagaStateMachineMessageActionType.When, message, consumeAction);
			_messageActions.Add(messageAction);
		}

		public void RecordCorrelateAction<TMessage>(Func<TMessage, bool> correlate)
			where TMessage : class, IMessage
		{
			var message = CreateMessage<TMessage>();

			Action<IMessage> correlateAction = (sagaMessage) =>
			                                   	{
			                                   		bool isCorrelated = correlate(sagaMessage as TMessage);

			                                   		if (isCorrelated == false)
			                                   		{
			                                   			throw new SagaMessageCouldNotBeCorrelatedToOngoingSagaException(
			                                   				sagaMessage.GetType(),
			                                   				_stateMachine.GetType(),
			                                   				_stateMachine.InstanceId);
			                                   		}
			                                   	};

			var messageAction = new MessageAction(SagaStateMachineMessageActionType.Correlate, message, correlateAction);
			_messageActions.Add(messageAction);
		}

		public void RecordDoAction<TMessage>(Action<TMessage> action, string notes = "")
			where TMessage : class, IMessage
		{
			var message = CreateMessage<TMessage>();
			Action<IMessage> doAction = theMessage => action(theMessage as TMessage);
			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Do, message, doAction) {Note = notes});
		}

		public void RecordPublishActionByType<TSagaMessage, TMessage>(Action<TSagaMessage, TMessage> publish)
			where TMessage : IMessage
			where TSagaMessage : IMessage
		{
			var message = CreateMessage<TMessage>();

			Action<IMessage> publishAction = (sagaMessage) =>
			                                 	{
			                                 		if (!_isInMockedState)
			                                 		{
			                                 			if (publish != null)
			                                 			{
			                                 				publish((TSagaMessage) sagaMessage, message);
			                                 				_stateMachine.Bus.Publish(message);
			                                 			}
			                                 			else
			                                 			{
			                                 				_stateMachine.Bus.Publish<TMessage>();
			                                 			}
			                                 		}
			                                 	};

			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Publish, message, publishAction));
		}

		public void RecordPublishAction<TMessage, TMessageToPublish>(Action<TMessage, TMessageToPublish> action)
			where TMessage : class, IMessage
			where TMessageToPublish : IMessage
		{
			var message = CreateMessage<TMessageToPublish>();

			Action<IMessage> publishAction = (sagaMessage) =>
			                                 	{
			                                 		if (!_isInMockedState)
			                                 		{
			                                 			action(sagaMessage as TMessage, message);
			                                 			_stateMachine.Bus.Publish(message);
			                                 		}
			                                 	};

			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Publish, message, publishAction));
		}


		public void RecordSendAction<TMessage, TMessageToSend>(Action<TMessage, TMessageToSend> action)
			where TMessage : class, IMessage
			where TMessageToSend : IMessage
		{
			var message = CreateMessage<TMessageToSend>();

			Action<IMessage> sendAction = (sagaMessage) =>
			                              	{
			                              		if (_isInMockedState == false)
			                              		{
			                              			action(sagaMessage as TMessage, message);
			                              			_stateMachine.Bus.Send(message);
			                              		}
			                              	};

			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Send, message, sendAction));
		}

		public void RecordSendToEndpointAction<TMessage, TMessageToSend>(Action<TMessage, TMessageToSend> action,
		                                                                 Uri endpoint)
			where TMessage : class, IMessage
			where TMessageToSend : IMessage
		{
			var message = CreateMessage<TMessageToSend>();

			Action<IMessage> sendAction = (sagaMessage) =>
			                              	{
			                              		if (!_isInMockedState)
			                              		{
			                              			action(sagaMessage as TMessage, message);
			                              			_stateMachine.Bus.Send(endpoint, message);
			                              		}
			                              	};

			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.SendToEndpoint,
			                                      message, sendAction) {Endpoint = endpoint.OriginalString});
		}

		public void RecordReplyAction<TMessage, TMessageToReplyWith>(Action<TMessage, TMessageToReplyWith> action)
			where TMessage : class, IMessage
			where TMessageToReplyWith : IMessage
		{
			var message = CreateMessage<TMessageToReplyWith>();

			Action<IMessage> replyAction = (sagaMessage) =>
			                               	{
			                               		if (!_isInMockedState)
			                               		{
			                               			action(sagaMessage as TMessage, message);
			                               			_stateMachine.Bus.Reply(message);
			                               		}
			                               	};
			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Reply, message, replyAction));
		}

		public void RecordDelayAction<TMessage, TMessageToDefer>(Action<TMessage, TMessageToDefer> action,
		    TimeSpan duration)
			where TMessage : class, IMessage
			where TMessageToDefer : IMessage
		{
			var message = CreateMessage<TMessageToDefer>();

			Action<IMessage> delayAction = (sagaMessage) =>
			                               	{
			                               		if (!_isInMockedState)
			                               		{
			                               			action(sagaMessage as TMessage, message);
			                               			_stateMachine.RequestTimeout(duration, message);
			                               			//_stateMachine.Bus.HandleMessageLater(duration, message);
			                               		}
			                               	};

			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Delay,
			                                      message, delayAction) {Delay = duration});
		}

		public void RecordTransitionAction<TMessage>(State newState)
			where TMessage : class, IMessage
		{
			Action<IMessage> transitionAction = (sagaMessage) => { _stateMachine.MarkTransitionTo(newState); };

			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Transition,
			                                      null, transitionAction) {State = newState});
		}

		public void RecordCompleteAction<TMessage>()
			where TMessage : class, IMessage
		{
			Action<IMessage> completeAction = (sagaMessage) =>
			                                  	{
			                                  		_stateMachine.MarkTransitionTo(new End());
			                                  		_stateMachine.MarkAsCompleted();
			                                  	};
			_messageActions.Add(new MessageAction(SagaStateMachineMessageActionType.Complete, null, completeAction));
		}

		#endregion

		private TMessage CreateMessage<TMessage>()
		{
			TMessage message = default(TMessage);

			if (typeof (TMessage).IsInterface)
			{
				if (!_isInMockedState)
				{
					message = _stateMachine.Bus.CreateMessage<TMessage>();
				}
				else
				{
					message = DefaultReflection.Factory.CreateReflection().CreateMessage<TMessage>();
				}
			}
			else
			{
				message = (TMessage) typeof (TMessage).Assembly.CreateInstance(typeof (TMessage).FullName);
			}

			return message;
		}
	}
}