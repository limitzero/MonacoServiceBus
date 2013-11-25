using System;
using System.Collections.Generic;

namespace Monaco.Sagas.StateMachine
{
	/// <summary>
	/// Trigger conditions on a saga for the current saga message
	/// </summary>
	/// <typeparam name="TSagaMessage">Type of the saga message being processed.</typeparam>
	public class SagaEventTriggerCondition<TSagaMessage> : ISagaEventTriggerCondition
		where TSagaMessage : class, IMessage
	{
		private readonly StateMachine _stateMachine;

		public IMessageActionRecorder Recorder { get; private set; }

		public IMessage Message { get; private set; }

		/// <summary>
		/// Gets the set of message actions that will be taken by the state machine.
		/// </summary>
		public ICollection<MessageAction> MessageActions { get { return this.Recorder.MessageActions; } }

		/// <summary>
		/// Gets or sets the state that the saga instance has transitioned into.
		/// </summary>
		public State State { get; set; }

		public string Event { get; private set; }

		public SagaEventTriggerCondition(StateMachine stateMachine, 
			IMessageActionRecorder recorder, 
			TSagaMessage currentMessage, 
			string @event)
		{
			this._stateMachine = stateMachine;
			this.Message = currentMessage;
			this.Event = @event;
			this.Recorder = recorder;

			// force the state machine to consume the current message that matches the event condition:
			this.Recorder.RecordConsumeAction(this.Message);
		}

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
			this.Recorder.RecordCorrelateAction(correlate);
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
			this.Recorder.RecordDoAction(action, notes);
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
			this.Recorder.RecordPublishActionByType<TSagaMessage, TMessage>(null);
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
			if(typeof(TMessage).IsInterface == true)
			{
				this.Recorder.RecordPublishActionByType<TSagaMessage, TMessage>(publish);
			}
			else
			{
				this.Recorder.RecordPublishAction(publish);	
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
			where TMessage :  IMessage
		{
			Action<TSagaMessage, TSagaMessage> send = (saga, message) => { };
			this.Recorder.RecordSendAction(send);
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
			where TMessage :  IMessage
		{
			this.Recorder.RecordSendAction(send);
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
			where TMessage :  IMessage
		{
			this.Recorder.RecordSendToEndpointAction(send, endpoint);
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
			this.Recorder.RecordReplyAction(reply);
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
			where TMessage :  IMessage
		{
			this.Recorder.RecordDelayAction(delay, duration);
			return this;
		}

		/// <summary>
		/// This will signal for the current saga to complete its processing.
		/// </summary>
		/// <returns></returns>
		public SagaEventTriggerCondition<TSagaMessage> Complete()
		{
			this.State = new State("End");
			this.Recorder.RecordCompleteAction<TSagaMessage>();
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
			this.State = state;
			this.Recorder.RecordTransitionAction<TSagaMessage>(this.State);
			return this;
		}
	}
}