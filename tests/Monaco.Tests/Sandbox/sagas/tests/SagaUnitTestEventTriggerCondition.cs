using System;
using System.Collections.Generic;
using Monaco.StateMachine;
using Rhino.Mocks;

namespace Monaco.Tests.Sandbox.sagas.tests
{
	public class SagaUnitTestEventTriggerCondition
	{
		private readonly SagaStateMachine _stateMachine;
		private readonly IServiceBus _serviceBus;

		/// <summary>
		/// Gets the actions that the saga state machine will execute.
		/// </summary>
		public List<Action> Actions { get; private set; }

		/// <summary>
		/// Gets the actions that make up the part of the mocked expectations.
		/// </summary>
		public List<Action> ExpectedActions { get; private set; }

		public SagaUnitTestEventTriggerCondition(SagaStateMachine stateMachine,
		                                         IServiceBus serviceBus,
		                                         Action when)
		{
			_stateMachine = stateMachine;
			_serviceBus = serviceBus;
			this.Actions = new List<Action>();
			this.Actions.Add(when);
			this.ExpectedActions = new List<Action>();
		}
		
		/// <summary>
		/// This will set the state machine to a desired state for testing.
		/// </summary>
		/// <param name="state"></param>
		public void SetSagaStateMachineState(State state)
		{
			_stateMachine.CurrentState = state;
		}

		/// <summary>
		/// This will create the expectation that the saga will publish a message to the interested parties.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="publish"></param>
		/// <returns></returns>
		public SagaUnitTestEventTriggerCondition ExpectToPublish<TMessage>(Func<TMessage> publish)
			where TMessage : IMessage, new()
		{
			var message = publish();

			Action thePublishAction = () => { }; // _sagaStateMachine.Bus.Publish(message);

			Action theExpectedAction = () =>
			                           	{
			                           		_serviceBus.Publish(message);
			                           		LastCall.Repeat.AtLeastOnce()
			                           			.Message(string.Format("The saga should publish the message '{0}'", typeof(TMessage).FullName));
			                           	};

			this.Actions.Add(thePublishAction);
			this.ExpectedActions.Add(theExpectedAction);

			return this;
		}

		/// <summary>
		///  This will create the expectation that the saga will run some custom logic.
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public SagaUnitTestEventTriggerCondition ExpectToDo(Action action)
		{
			this.Actions.Add(action);
			return this;
		}

		/// <summary>
		/// This will create the expectation that the saga will send a message to the message owner.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="send"></param>
		/// <returns></returns>
		public SagaUnitTestEventTriggerCondition ExpectToSend<TMessage>(Func<TMessage> send)
			where TMessage : IMessage
		{
			var message = send();

			Action theSendAction = () => _stateMachine.Bus.Send(message);

			Action theExpectedAction = () =>
			                           	{
			                           		_serviceBus.Send(message);
			                           		LastCall.Repeat.AtLeastOnce()
			                           			.Message(string.Format("The saga should send the message '{0}'", typeof(TMessage).FullName));
			                           	};

			this.Actions.Add(theSendAction);
			this.ExpectedActions.Add(theExpectedAction);

			return this;
		}

		/// <summary>
		/// This will create the expectation that the saga will send a message to a specific endpoint.
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="endpoint"></param>
		/// <param name="send"></param>
		/// <returns></returns>
		public SagaUnitTestEventTriggerCondition ExpectToSendToEndpoint<TMessage>(Uri endpoint, Func<TMessage> send)
			where TMessage : IMessage
		{
			var message = send();

			Action theSendAction = () => _stateMachine.Bus.Send(endpoint, message);

			Action theExpectedAction = () =>
			                           	{
			                           		_serviceBus.Send(message);
			                           		LastCall.Repeat.AtLeastOnce()
			                           			.Message(string.Format("The saga should send the message '{0}' to endpoint '{1}'.",
			                           			                       typeof(TMessage).FullName,
			                           			                       endpoint.OriginalString));
			                           	};

			this.Actions.Add(theSendAction);
			this.ExpectedActions.Add(theExpectedAction);

			return this;
		}

		/// <summary>
		/// This will set the expectation that the saga state machine will transition to a desired state after 
		/// the message is consumed.
		/// </summary>
		/// <typeparam name="TState"></typeparam>
		/// <returns></returns>
		public SagaUnitTestEventTriggerCondition ExpectTransitionTo<TState>()
			where TState : State, new()
		{
			Action theTransitionAction = () =>
			                             	{
			                             		if (_stateMachine.CurrentState.GetType() != typeof(TState))
			                             		{
			                             			throw new Exception(string.Format(
			                             				"The saga was expected to transition to state '{0} but is currently in state '{1}'.",
			                             				typeof(TState).Name,
			                             				_stateMachine.CurrentState.GetType().Name));
			                             		}
			                             	};

			this.Actions.Add(theTransitionAction);

			return this;
		}
	}
}