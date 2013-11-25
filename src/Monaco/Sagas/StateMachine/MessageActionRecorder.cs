using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Internals.Reflection.Impl;

namespace Monaco.Sagas.StateMachine
{
	public class MessageActionRecorder : IMessageActionRecorder
	{
		private bool _isInMockedState;
		private readonly StateMachine _stateMachine;
		private readonly List<MessageAction> _messageActions;

		public ICollection<MessageAction> MessageActions { get { return _messageActions; } }

		public MessageActionRecorder(StateMachine stateMachine)
		{
			_stateMachine = stateMachine;
			this._messageActions = new List<MessageAction>();
			this._isInMockedState = true;

			// if in mocked state, do not execute the bus actions for transmitting message:
			_isInMockedState = _stateMachine.Bus.GetType().FullName.ToLower().Trim().Contains("proxy");

		}

		public void Play(IMessage message)
		{
			// Transistion, When and Correlate are performed by the dispatcher:
			var actions = (from match in this.MessageActions
						   where match.ActionType != SagaMessageActionType.Transition
							&& match.ActionType != SagaMessageActionType.When
							&& match.ActionType != SagaMessageActionType.Correlate
						   select match).Distinct().ToList();

			// make the recorder playback in non-mocked mode:
			this._isInMockedState = false;

			// execute the actions on the state machine as recorded:
			actions.ForEach(x => x.Action(message));
		}

		public void RecordConsumeAction<TMessage>(TMessage message)
			where TMessage : class, IMessage
		{
			Action<IMessage> consumeAction = (sagaMessage) =>
													{
														((Consumes<TMessage>)_stateMachine)
															.Consume((TMessage)sagaMessage);
													};

			var messageAction = new MessageAction(SagaMessageActionType.When, message, consumeAction);
			this._messageActions.Add(messageAction);
		}

		public void RecordCorrelateAction<TMessage>(Func<TMessage, bool> correlate)
			where TMessage : class, IMessage
		{
			var message = this.CreateMessage<TMessage>();

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

			var messageAction = new MessageAction(SagaMessageActionType.Correlate, message, correlateAction);
			this._messageActions.Add(messageAction);
		}

		public void RecordDoAction<TMessage>(Action<TMessage> action, string notes = "")
			where TMessage : class, IMessage
		{
			var message = this.CreateMessage<TMessage>();
			Action<IMessage> doAction = theMessage => action(theMessage as TMessage);
			this._messageActions.Add(new MessageAction(SagaMessageActionType.Do, message, doAction) { Note = notes });
		}

		public void RecordPublishActionByType<TSagaMessage, TMessage>(Action<TSagaMessage, TMessage> publish)
			where TMessage : IMessage
			where TSagaMessage : IMessage
		{
			var message = this.CreateMessage<TMessage>();

			Action<IMessage> publishAction = (sagaMessage) =>
			{
				if (!_isInMockedState)
				{
					if (publish != null)
					{
						publish((TSagaMessage)sagaMessage, message);
						_stateMachine.Bus.Publish(message);
					}
					else
					{
						_stateMachine.Bus.Publish<TMessage>();
					}
				}
			};

			this._messageActions.Add(new MessageAction(SagaMessageActionType.Publish, message, publishAction));
		}

		public void RecordPublishAction<TMessage, TMessageToPublish>(Action<TMessage, TMessageToPublish> action)
			where TMessage : class, IMessage
			where TMessageToPublish : IMessage
		{
			var message = this.CreateMessage<TMessageToPublish>();

			Action<IMessage> publishAction = (sagaMessage) =>
														{
															if (!_isInMockedState)
															{
																action(sagaMessage as TMessage, message);
																_stateMachine.Bus.Publish(message);
															}
														};

			this._messageActions.Add(new MessageAction(SagaMessageActionType.Publish, message, publishAction));
		}

	
		public void RecordSendAction<TMessage, TMessageToSend>(Action<TMessage, TMessageToSend> action)
			where TMessage : class, IMessage
			where TMessageToSend : IMessage
		{
			var message = this.CreateMessage<TMessageToSend>();

			Action<IMessage> sendAction = (sagaMessage) =>
			{
				if (_isInMockedState == false)
				{
					action(sagaMessage as TMessage, message);
					_stateMachine.Bus.Send(message);
				}
			};

			this._messageActions.Add(new MessageAction(SagaMessageActionType.Send, message, sendAction));
		}

		public void RecordSendToEndpointAction<TMessage, TMessageToSend>(Action<TMessage, TMessageToSend> action,
			Uri endpoint)
			where TMessage : class, IMessage
			where TMessageToSend : IMessage
		{
			var message = this.CreateMessage<TMessageToSend>();

			Action<IMessage> sendAction = (sagaMessage) =>
			{
				if (!_isInMockedState)
				{
					action(sagaMessage as TMessage, message);
					_stateMachine.Bus.Send(endpoint, message);
				}
			};

			this._messageActions.Add(new MessageAction(SagaMessageActionType.SendToEndpoint,
				message, sendAction) { Endpoint = endpoint.OriginalString });
		}

		public void RecordReplyAction<TMessage, TMessageToReplyWith>(Action<TMessage, TMessageToReplyWith> action)
			where TMessage : class, IMessage
			where TMessageToReplyWith : IMessage
		{
			var message = this.CreateMessage<TMessageToReplyWith>();

			Action<IMessage> replyAction = (sagaMessage) =>
			{
				if (!_isInMockedState)
				{
					action(sagaMessage as TMessage, message);
					_stateMachine.Bus.Reply(message);
				}

			};
			this._messageActions.Add(new MessageAction(SagaMessageActionType.Reply, message, replyAction));
		}

		public void RecordDelayAction<TMessage, TMessageToDefer>(Action<TMessage, TMessageToDefer> action,
			TimeSpan duration)
			where TMessage : class, IMessage
			where TMessageToDefer : IMessage
		{
			var message = this.CreateMessage<TMessageToDefer>();

			Action<IMessage> delayAction = (sagaMessage) =>
			{
				if (!_isInMockedState)
				{
					action(sagaMessage as TMessage, message);
					_stateMachine.RequestTimeout(duration, message);
					//_stateMachine.Bus.HandleMessageLater(duration, message);
				}

			};

			this._messageActions.Add(new MessageAction(SagaMessageActionType.Delay,
				message, delayAction) { Delay = duration });
		}

		public void RecordTransitionAction<TMessage>(State newState)
				where TMessage : class, IMessage
		{
			Action<IMessage> transitionAction = (sagaMessage) =>
														{
															_stateMachine.MarkTransitionTo(newState);
														};

			this._messageActions.Add(new MessageAction(SagaMessageActionType.Transition,
				null, transitionAction) { State = newState });
		}

		public void RecordCompleteAction<TMessage>()
			where TMessage : class, IMessage
		{
			Action<IMessage> completeAction = (sagaMessage) =>
													{
														_stateMachine.MarkTransitionTo(new EndState());
														_stateMachine.MarkAsCompleted();
													};
			this._messageActions.Add(new MessageAction(SagaMessageActionType.Complete, null, completeAction));
		}

		private TMessage CreateMessage<TMessage>()
		{
			var message = default(TMessage);

			if (typeof(TMessage).IsInterface)
			{
				if (!_isInMockedState)
				{
					message = _stateMachine.Bus.CreateMessage<TMessage>();
				}
				else
				{
					message = DefaultReflection.CreateMessage<TMessage>();
				}
			}
			else
			{
				message = (TMessage)typeof(TMessage).Assembly.CreateInstance(typeof(TMessage).FullName);
			}

			return message;
		}
	}
}