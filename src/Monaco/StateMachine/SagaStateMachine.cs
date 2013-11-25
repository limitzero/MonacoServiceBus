using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;
using Monaco.Bus.Messages.For.Sagas;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.StateMachine.Internals;
using Monaco.StateMachine.Internals.Impl;
using Monaco.StateMachine.Roles;

namespace Monaco.StateMachine
{
	/* State Machine Syntax:
	 * 
	 * Initially(
	 *	   When(<some event>, 
	 *				.Do( (sagaMessage) => { // some code actions here for logic })
	 *				.Publish<SomeMessage>( (sagaMessage, publish) => { // construct message to publish})
	 *				.Send<SomeMessage>( (sagaMessage, send) => { // construct message to send })
	 *				.Delay<SomeMessage>( (sagaMessage, defer, Timespan) => { // construct message to defer})
	 *				.TransitionTo(<some other state>)
	 *				.Complete()
	 *			));
	 *			
	 * Also(
	 *	   When(<some event>, 
	 *				.Do( (sagaMessage) => { // some code actions here for logic })
	 *				.Publish<SomeMessage>( (sagaMessage, publish) => { // construct message to publish})
	 *				.Send<SomeMessage>( (sagaMessage, send) => { // construct message to send })
	 *				.Delay<SomeMessage>( (sagaMessage, defer, Timespan) => { // construct message to defer})
	 *				.TransitionTo(<some other state>)
	 *				.Complete()
	 *			));
	 *			
	 * While(<in some state>, 
	 *			When(<some event>, 
	 *				.Correlate( (sagamessage)=> { sagamessage.CorrelationId = Data.CorrelationId; } ) // tie the message to current converstation
	 *				.Do( (sagaMessage) => { // some code actions here for logic })
	 *				.Publish<SomeMessage>( (sagaMessage, publish) => { // construct message to publish})
	 *				.Send<SomeMessage>( (sagaMessage, send) => { // construct message to send })
	 *				.Delay<SomeMessage>( (sagaMessage, defer, Timespan) => { // construct message to defer})
	 *				.TransitionTo(<some other state>)
	 *				.Complete()
	 *		));
	 * 
	 * 
	 * Reads:
	 * Initially, when some event happens, do some action, publish a message, send a message, defer publication, 
	 * transition to a state, complete
	 * 
	 * Also, when some event happens, do some action, publish a message, send a message, defer publication, 
	 * transition to a state, complete
	 * 
	 * While in a state, when some event happens, do some action, publish a message, send a message, defer publication, 
	 * transition to a state, complete
	 */

	public abstract class SagaStateMachine : IStateMachine
	{
		public Action<bool> OnSuspendedEvent;

		[XmlIgnore] private ISagaEventTriggerCondition _condition;

		protected SagaStateMachine()
		{
			TriggerConditions = new List<SagaStateMachineDefinedTriggerCondition>();
			PrepareStatesForStateMachine();
			StateMachineDataToMessageCorrelationExpressions =
				new List<StateMachineDataToMessageDataCorrelation>();
		}

		/// <summary>
		/// Gets or sets the current instance of the <seealso cref="IServiceBus"/> for the saga instance.
		/// </summary>
		[XmlIgnore]
		public IServiceBus Bus { get; set; }

		/// <summary>
		/// Gets the set of trigger conditions that can happen on the saga instance when a message is received.
		/// </summary>
		[XmlIgnore]
		public List<SagaStateMachineDefinedTriggerCondition> TriggerConditions { get; private set; }

		/// <summary>
		/// Gets or sets the name for the saga state machine.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the current state of the saga state machine.
		/// </summary>
		public State CurrentState { get; set; }

		/// <summary>
		/// Gets or sets the message that the saga is currently consuming.
		/// </summary>
		public IMessage CurrentMessage { get; set; }

		[XmlIgnore]
		public IList<StateMachineDataToMessageDataCorrelation> StateMachineDataToMessageCorrelationExpressions { get; private set; }

		#region IStateMachine Members

		/// <summary>
		/// Gets or sets the instance identifier for the saga instance.
		/// </summary>
		public Guid InstanceId { get; set; }

		/// <summary>
		/// Gets or sets the flag to indicate whether or not the process has completed.
		/// </summary>
		public bool IsCompleted { get; set; }

		#endregion

		/// <summary>
		/// This will reset the trigger conditions for the state machine in transitioning
		/// from one state to the next.
		/// </summary>
		public void Reset()
		{
			TriggerConditions = new List<SagaStateMachineDefinedTriggerCondition>();
		}

		/// <summary>
		/// This is the marker for defining the state machine and its events for handling messages.
		/// </summary>
		public abstract void Define();

		/// <summary>
		/// This will mark the begining of the saga instance, which usually maps to the <seealso cref="StartedBy{T}"/>
		/// contract for message consumption.
		/// </summary>
		/// <param name="condition">"When" condition that starts actions for processing</param>
		public void Initially(ISagaEventTriggerCondition condition)
		{
			// add the event trigger condition to the collection and mark it as "Initially";
			_condition.State = new State("Start");

			if (CurrentState != null)
			{
				CurrentState = _condition.State;
			}

			var definedTriggerCondition = new SagaStateMachineDefinedTriggerCondition(SagaStateMachineStageType.Initially,
			                                                                          _condition);

			if (TriggerConditions.Exists(x => x.Stage == SagaStateMachineStageType.Initially))
				throw new Exception("The saga " + GetType().FullName +
				                    " can not be configured with multiple initial conditons for starting the saga.");

			TriggerConditions.Add(definedTriggerCondition);
		}

		/// <summary>
		/// This will mark the subsequent processing of the saga instance, which usually maps 
		/// to the <seealso cref="OrchestratedBy{T}"/> contract for message consumption with
		/// only the arrival of a message and no state transition is needed.
		/// </summary>
		/// <param name="condition">"When" condition that starts actions for processing</param>
		public void Also(ISagaEventTriggerCondition condition)
		{
			var definedTriggerCondition = new SagaStateMachineDefinedTriggerCondition(SagaStateMachineStageType.Also,
			                                                                          _condition);
			_condition.State = null;
			TriggerConditions.Add(definedTriggerCondition);
		}

		/// <summary>
		/// This will mark the subsequent processing of the saga instance, which usually maps 
		/// to the <seealso cref="OrchestratedBy{T}"/> contract for message consumption after a 
		/// particular state has been reached.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="condition">"When" condition that starts actions for processing</param>
		public void While(State state, ISagaEventTriggerCondition condition)
		{
			// add the event trigger condition to the collection, set the state, and mark it as "While";
			_condition.State = state;

			var definedTriggerCondition = new SagaStateMachineDefinedTriggerCondition(
				SagaStateMachineStageType.While,
			    condition);

			TriggerConditions.Add(definedTriggerCondition);
		}

		/// <summary>
		/// This will mark the subsequent processing of the saga instance, which usually maps 
		/// to the <seealso cref="OrchestratedBy{T}"/> contract for message consumption after a 
		/// particular state has been reached.
		/// </summary>
		/// <param name="precondition">expression that evaluates to a boolean for a precondition on executing the action for the condition.</param>
		/// <param name="preconditionExceptionMessage"></param>
		/// <param name="condition">"When" condition that starts actions for processing</param>
		public void While(Expression<Func<bool>> precondition,
			string preconditionExceptionMessage,
			ISagaEventTriggerCondition condition)
		{
			// add the event trigger condition to the collection, set the state, and mark it as "While";
			_condition.State = new State("[[Evaluated Pre-Condition]]");
			_condition.PreCondition = precondition;

			var definedTriggerCondition = 
				new SagaStateMachineDefinedTriggerCondition(SagaStateMachineStageType.While,
				condition);

			TriggerConditions.Add(definedTriggerCondition);
		}

		/// <summary>
		/// This signals an event where a message is received on the saga state machine 
		/// and processing will begin in accordance to what the message means.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="when"></param>
		/// <returns></returns>
		public SagaEventTriggerCondition<TMessage> When<TMessage>(Event<TMessage> when)
			where TMessage : class, IMessage
		{
			string @event = FindNameForEvent(typeof (Event<TMessage>));
			var recorder = new MessageActionRecorder(this);

			TMessage message = default(TMessage);

			if (typeof (TMessage).IsInterface)
			{
				message = Bus.CreateMessage<TMessage>();
			}
			else
			{
				// do a raw message instantiation:
				message = typeof (TMessage).Assembly.CreateInstance(typeof (TMessage).FullName) as TMessage;
			}

			_condition = new SagaEventTriggerCondition<TMessage>(this, recorder, message, @event);

			// force the saga to consume the current message that matches the event condition:
			//_condition = new SagaEventTriggerCondition<TMessage>(this,
			//    new TMessage(),
			//   () => ((Consumes<TMessage>)this).Consume((TMessage)this.CurrentMessage),
			//   @event);

			return _condition as SagaEventTriggerCondition<TMessage>;
		}

		/// <summary>
		/// This signals the state machine to defer publication of a message 
		/// by a given timeframe via the service bus. 
		/// </summary>
		/// <param name="duration">Duration to wait before delivering the message</param>
		/// <param name="message">Message to deliver on a delayed interval.</param>
		public abstract void RequestTimeout(TimeSpan duration, IMessage message);

		/// <summary>
		/// This signals the state machine to defer publication of a message 
		/// by a given timeframe via the service bus. 
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to create.</typeparam>
		/// <param name="duration">Duration to wait before delivering the message</param>
		/// <param name="create">Lambda to create message to deliver on a delayed interval.</param>
		public abstract void RequestTimeout<TMessage>(TimeSpan duration, Action<TMessage> create) where TMessage : IMessage;

		/// <summary>
		/// This will mark the saga as "completed" and signal the removal of the instance
		/// from the persistance store.
		/// </summary>
		public void MarkAsCompleted()
		{
			IsCompleted = true;
		}

		/// <summary>
		/// This will allow the current state machine to be forced to transition to a new state.
		/// </summary>
		public State MarkTransitionTo(State state)
		{
			CurrentState = state;
			return state;
		}

		/// <summary>
		/// This will suspend this saga state machine for a given duration. After the duration has passed, 
		/// the <seealso cref="OnSuspendCompleted"/> method will be called on the saga instance
		/// to process any state that was passed from the initial suspend request.
		/// </summary>
		/// <param name="duration"></param>
		/// <param name="state"></param>
		public void Suspend(TimeSpan duration, object state = null)
		{
			var suspendMessage = new SuspendSagaMessage {InstanceId = InstanceId, State = state, Saga = GetType()};
			Bus.ConsumeMessages(new ScheduleTimeout(InstanceId, duration, suspendMessage));
		}

		/// <summary>
		///  This will be called after the saga state machine has been 
		///  resumed after the suspension period.
		/// </summary>
		/// <param name="state">Any serializable state that can be passed to the state machine after suspension</param>
		public void SuspendCompleted(object state)
		{
			OnSuspendCompleted(state);
		}

		/// <summary>
		///  This will be called after the saga state machine has been 
		///  resumed after the suspension period.
		/// </summary>
		/// <param name="state"></param>
		public virtual void OnSuspendCompleted(object state)
		{
		}

		/// <summary>
		/// This will enable the state machine to retreive the instance 
		/// data given a mapping of a common property on the instance data 
		/// for the state machine and a similiar property on the message 
		/// being handled by the state machine via the <seealso cref="StartedBy{T}"/>
		/// or the <seealso cref="OrchestratedBy{T}"/> interfaces. Use the 
		/// method "CorrelateMessageToStateMachineData{T}" to create the lambda
		/// expression to match the instance data to the message data.
		/// </summary>
		public virtual void ConfigureHowToFindStateMachineInstanceDataFromMessages()
		{
		}

		/// <summary>
		/// This will initialize all of the states on the state machine and set the name 
		/// of the <seealso cref="State"/> to the name of the property defining the 
		/// state.
		/// </summary>
		private void PrepareStatesForStateMachine()
		{
			List<PropertyInfo> properties = (from property in GetType().GetProperties()
			                                 where typeof (State).IsAssignableFrom(property.PropertyType)
			                                       && property.Name != "CurrentState"
			                                 select property).Distinct().ToList();

			// create the state with the name based on the property:
			foreach (PropertyInfo property in properties)
			{
				var state = new State(property.Name);
				property.SetValue(this, state, null);
			}
		}

		/// <summary>
		/// This will find the name of the property that defines an event (message reception) 
		/// for the state machine.
		/// </summary>
		/// <param name="event"></param>
		/// <returns></returns>
		private string FindNameForEvent(Type @event)
		{
			string eventName = string.Empty;

			PropertyInfo eventProperty = (from property in GetType().GetProperties()
			                              where property.PropertyType == @event
			                              select property).FirstOrDefault();

			if (eventProperty != null)
			{
				eventName = eventProperty.Name;
			}

			return eventName;
		}
	}

	/// <summary>
	/// Base implementation of a long-running process with a defined persisted 
	/// data entity for keeping data between long-running process calls.
	/// </summary>
	/// <typeparam name="TData">Type of the data/state to keep persisted between calls.</typeparam>
	[Serializable]
	public abstract class SagaStateMachine<TData> : SagaStateMachine, IDisposable
		where TData : class, IStateMachineData, new()
	{
		protected SagaStateMachine()
		{
			Correlations = new Dictionary<Type, Expression<Func<TData, IMessage, bool>>>();
		}

		/// <summary>
		/// Gets or sets the data/state associated with the saga state machine.
		/// </summary>
		public TData Data { get; set; }

		/// <summary>
		/// Gets the set of correlations that define how a message should be matched to the current saga instance.
		/// </summary>
		[XmlIgnore]
		public IDictionary<Type, Expression<Func<TData, IMessage, bool>>> Correlations { get; private set; }

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion

		/// <summary>
		/// This signals the state machine to defer publication of a message 
		/// by a given timeframe via the service bus. 
		/// </summary>
		/// <param name="duration">Duration to wait before delivering the message</param>
		/// <param name="message">Message to deliver on a delayed interval.</param>
		public override void RequestTimeout(TimeSpan duration, IMessage message)
		{
			var scheduledTimeout = new ScheduleTimeout(duration, message)
			                       	{
			                       		RequestorId = this.Data.Id,
			                       		Requestor = this.GetType().Name
			                       	};
			Bus.Send(scheduledTimeout);
		}

		/// <summary>
		/// This signals the state machine to defer publication of a message 
		/// by a given timeframe via the service bus. 
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to create.</typeparam>
		/// <param name="duration">Duration to wait before delivering the message</param>
		/// <param name="create">Lambda to create message to deliver on a delayed interval.</param>
		public override void RequestTimeout<TMessage>(TimeSpan duration, Action<TMessage> create)
		{
			var message = Bus.CreateMessage<TMessage>();

			if (create != null)
				create(message);

			RequestTimeout(duration, message);
		}

		/// <summary>
		/// This will create an expression used to correlate a message to the state machine data for retreival.
		/// </summary>
		/// <example>
		///  CorrelateMessageToStateMachineData{MyMessage}(statemachine => statemachine.CustomerId, message => message.CustomerId);
		/// </example>
		/// <typeparam name="TMessage">Current message type to correlate</typeparam>
		/// <param name="stateMachineDataExpression">Expression used to determine the instance data property to be matched by message</param>
		/// <param name="messageExpression">Expression used to determine the property that should match the instance data property</param>
		protected void CorrelateMessageToStateMachineData<TMessage>(
			Expression<Func<TData, object>> stateMachineDataExpression,
			Expression<Func<TMessage, object>> messageExpression)
			where TMessage : IMessage
		{
			string stateMachinDataPropertyName = GetPropertyNameFromExpression(stateMachineDataExpression);
			string messagePropertyName = GetPropertyNameFromExpression(messageExpression);

			var stateMachineToMessageCorrelation =
				new StateMachineDataToMessageDataCorrelation(typeof (TMessage),
				                                             stateMachinDataPropertyName,
				                                             messagePropertyName);

			if (StateMachineDataToMessageCorrelationExpressions
			    	.Contains(stateMachineToMessageCorrelation) == false)
			{
				StateMachineDataToMessageCorrelationExpressions.Add(stateMachineToMessageCorrelation);
			}

			/*	 -- this works --
			var startedByType = typeof (StartedBy<>).MakeGenericType(typeof (TMessage));

			if(!typeof(StartedBy<TMessage>).IsAssignableFrom(startedByType))
			    throw new InvalidOperationException(string.Format("The message '{0}' can not be used to correlate " + 
			        "instances of the state machine. Please use the message that is defined in the declaration '{1}',", 
			        typeof(TMessage).Name,
			        startedByType.Name));

			this.CurrentInitializationMappingConfigurationExpression = () =>
			{
			    // need to know if the comparision is not the same, this triggers a new 
			    // instance of the instance data for the state machine!!!
			    return
			        stateMachineDataExpression.Compile().Invoke(this.Data) !=
			        messageExpression.Compile().Invoke(
			            (TMessage) this.CurrentMessage);
			};
			*/
		}

		/// <summary>
		/// This will be executed by the dispatcher to see if the current 
		/// message is correlated to the given instance of the state machine's
		/// data configuration.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public bool IsCorrelated(IMessage message)
		{
			bool result = false;
			Expression<Func<TData, IMessage, bool>> correlation = Correlations[message.GetType()];
			if (correlation == null) return result;

			result = correlation.Compile()(Data, message);
			return result;
		}

		private string GetPropertyNameFromExpression<TEntity>(Expression<Func<TEntity, object>> expression)
		{
			MemberExpression memberExpression;

			if (expression.Body is UnaryExpression)
			{
				memberExpression = ((UnaryExpression) expression.Body).Operand as MemberExpression;
			}
			else
			{
				memberExpression = expression.Body as MemberExpression;
			}

			if (memberExpression == null)
			{
				throw new InvalidOperationException("You must specify a property!");
			}

			return memberExpression.Member.Name;
		}
	}
}