using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Monaco.StateMachine.Internals.Impl
{
	/// <summary>
	/// Trigger conditions on a saga for the current saga message
	/// </summary>
	/// <typeparam name="TSagaMessage">Type of the saga message being processed.</typeparam>
	public class SagaEventTriggerCondition<TSagaMessage> : ISagaEventTriggerCondition
		where TSagaMessage : class, IMessage
	{
		private readonly SagaStateMachine _stateMachine;

		public SagaEventTriggerCondition(SagaStateMachine stateMachine,
		                                 IMessageActionRecorder recorder,
		                                 TSagaMessage currentMessage,
		                                 string @event)
		{
			_stateMachine = stateMachine;
			Message = currentMessage;
			Event = @event;
			Recorder = recorder;

			// force the state machine to consume the current message that matches the event condition:
			Recorder.RecordConsumeAction(Message);
		}

		/// <summary>
		/// Gets the set of message actions that will be taken by the state machine.
		/// </summary>
		public ICollection<MessageAction> MessageActions
		{
			get { return Recorder.MessageActions; }
		}

		public Expression<Func<bool>> PreCondition { get; set; }

		#region ISagaEventTriggerCondition Members

		public IMessageActionRecorder Recorder { get; private set; }

		public IMessage Message { get; private set; }

		/// <summary>
		/// Gets or sets the state that the saga instance has transitioned into.
		/// </summary>
		public State State { get; set; }

		public string Event { get; private set; }

		#endregion

		/// <summary>
		/// This will signal the state machine to check the current message for correlation 
		/// to the instance data to see if this message can be accepted onto the saga for 
		/// processing.
		/// </summary>
		/// <typeparam name="TSagaMessage"></typeparam>
		/// <param name="correlate"></param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> CorrelatedBy(Func<TSagaMessage, bool> correlate)
		{
			Recorder.RecordCorrelateAction(correlate);
			return this;
		}

		/// <summary>
		/// This will signal that the saga state machine can execute some custom code after a message 
		/// is received.
		/// </summary>
		/// <param name="action">Custom code to execute</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Do(Action<TSagaMessage> action)
		{
			return Do(action, string.Empty);
		}

		/// <summary>
		/// This will signal that the saga state machine can execute some custom code after a message 
		/// is received.
		/// </summary>
		/// <param name="action">Custom code to execute</param>
		/// <param name="notes">Custom documentation for the current declaration of custom code</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Do(Action<TSagaMessage> action, string notes = "")
		{
			Recorder.RecordDoAction(action, notes);
			return this;
		}

		/// <summary>
		/// This will signal that the saga instance will publish a message to one or more message consumers.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to publish</typeparam>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Publish<TMessage>()
			where TMessage : IMessage
		{
			Recorder.RecordPublishActionByType<TSagaMessage, TMessage>(null);
			return this;
		}

		/// <summary>
		/// This will signal that the saga instance will publish a message to one or more message consumers.
		/// </summary>
		/// <typeparam name="TMessage">Concrete type of the message to publish</typeparam>
		/// <param name="publish">Function to create the message to publish</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Publish<TMessage>(Action<TSagaMessage, TMessage> publish)
			where TMessage : IMessage
		{
			if (typeof (TMessage).IsInterface)
			{
				Recorder.RecordPublishActionByType(publish);
			}
			else
			{
				Recorder.RecordPublishAction(publish);
			}

			return this;
		}

		/// <summary>
		/// This will signal that the saga instance will send a message to a consumer on the local bus 
		/// instance.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to send</typeparam>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Send<TMessage>()
			where TMessage : IMessage
		{
			Action<TSagaMessage, TSagaMessage> send = (saga, message) => { };
			Recorder.RecordSendAction(send);
			return this;
		}

		/// <summary>
		/// This will signal that the saga instance will send a message to a consumer on the local bus 
		/// instance.
		/// </summary>
		/// <typeparam name="TMessage">Concrete type of the message to send</typeparam>
		/// <param name="send">Function to create the message to send</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Send<TMessage>(Action<TSagaMessage, TMessage> send)
			where TMessage : IMessage
		{
			Recorder.RecordSendAction(send);
			return this;
		}

		/// <summary>
		/// This will signal that the saga instance will send a message to a consumer on the local bus 
		/// instance.
		/// </summary>
		/// <typeparam name="TMessage">Concrete type of the message to send</typeparam>
		/// <param name="endpoint">The uri semantic for a location that will process the message</param>
		/// <param name="send">Function to create the message to send</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> SendToEndpoint<TMessage>(Uri endpoint,
		                                                                        Action<TSagaMessage, TMessage> send)
			where TMessage : IMessage
		{
			Recorder.RecordSendToEndpointAction(send, endpoint);
			return this;
		}

		/// <summary>
		/// This will signal that the saga instance will send a reply for the current message being 
		/// processed.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message representing the reply</typeparam>
		/// <param name="reply">Function to return the new reply message</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Reply<TMessage>(Action<TSagaMessage, TMessage> reply)
			where TMessage : IMessage
		{
			Recorder.RecordReplyAction(reply);
			return this;
		}

		/// <summary>
		/// This will signal that a message will be delayed for publication after a given interval.
		/// </summary>
		/// <typeparam name="TMessage">Concrete message to schedule for delayed delivery</typeparam>
		/// <param name="duration">Time to wait before delivery</param>
		/// <param name="delay">Function to create the message for delivery</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Delay<TMessage>(TimeSpan duration,
		                                                               Action<TSagaMessage, TMessage> delay)
			where TMessage : IMessage
		{
			Recorder.RecordDelayAction(delay, duration);
			return this;
		}

		/// <summary>
		/// This will signal for the current saga to complete its processing.
		/// </summary>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Complete()
		{
			State = new State("End");
			Recorder.RecordCompleteAction<TSagaMessage>();
			return this;
		}

		/// <summary>
		/// This will signal that the current saga will transition to a pre-defined state and wait
		/// for a new event to be triggered for re-starting the processing
		/// </summary>
		/// <param name="state">The concrete state to set on the saga</param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> TransitionTo(State state)
		{
			State = state;
			Recorder.RecordTransitionAction<TSagaMessage>(State);
			return this;
		}
	}
}