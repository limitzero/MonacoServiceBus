using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Castle.MicroKernel;
using Monaco.Bus;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines;
using Monaco.Configuration;
using Monaco.StateMachine;
using Monaco.Testing.Internals.Exceptions;
using Monaco.Testing.StateMachines.Internals.Actions.Impl;

namespace Monaco.Testing.StateMachines.Impl
{
	public class StateMachineTestScenario<TStateMachine> :
		IStateMachineTestScenario<TStateMachine>
		where TStateMachine : SagaStateMachine
	{
		private readonly List<Action> _postconditions;
		private readonly List<Action<IMessage>> _verifiables;

		public StateMachineTestScenario(
			IMessage consumedMessage,
			Action consumeMessageAction,
			TStateMachine stateMachine,
			IContainer container,
			IServiceBus mockServiceBus)
			: this(consumedMessage, null, consumeMessageAction, stateMachine, container, mockServiceBus)
		{
		}

		public StateMachineTestScenario(
			IMessage consumedMessage,
			Func<IMessage> externalConstructMessageAction,
			Action consumeMessageAction,
			TStateMachine stateMachine,
			IContainer container,
			IServiceBus mockServiceBus)
		{
			ConsumedMessage = consumedMessage;
			ExternalConstructMessageAction = externalConstructMessageAction;
			ConsumeMessageAction = consumeMessageAction;
			StateMachine = stateMachine;
			Container = container;
			MockServiceBus = mockServiceBus;

			_verifiables = new List<Action<IMessage>>();
			_postconditions = new List<Action>();
		}

		protected IServiceBus MockServiceBus { get; private set; }
		protected TStateMachine StateMachine { get; private set; }
		protected IContainer Container { get; set; }
		protected IMessage ConsumedMessage { get; set; }
		protected Func<IMessage> ExternalConstructMessageAction { get; set; }
		protected Action ConsumeMessageAction { get; set; }

		#region IStateMachineTestScenario<TStateMachine> Members

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToPublish<T>() where T : IMessage
		{
			return ExpectToPublish<T>(null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToPublish<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectToPublishAction<T, TStateMachine>(Container,
			                                                                      StateMachine,
			                                                                      MockServiceBus,
			                                                                      ConsumedMessage,
			                                                                      messageConstructionAction).CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToPublish<T>() where T : IMessage
		{
			return ExpectNotToPublish<T>(null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToPublish<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectNotToPublishAction<T, TStateMachine>(Container,
			                                                                         StateMachine,
			                                                                         MockServiceBus,
			                                                                         ConsumedMessage,
			                                                                         messageConstructionAction).                         
				CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToSend<T>() where T : IMessage
		{
			return ExpectToSend<T>(null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToSend<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectToSendAction<T, TStateMachine>(Container,
			                                                                   StateMachine,
			                                                                   MockServiceBus,
			                                                                   ConsumedMessage,
			                                                                   messageConstructionAction).
				CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToSend<T>() where T : IMessage
		{
			return ExpectNotToSend<T>(null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToSend<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectNotToSendAction<T, TStateMachine>(Container,
			                                                                      StateMachine,
			                                                                      MockServiceBus,
			                                                                      ConsumedMessage,
			                                                                      messageConstructionAction)
				.CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToSendEndpoint<T>(Uri endpoint) where T : IMessage
		{
			return ExpectToSendEndpoint<T>(endpoint);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToSendEndpoint<T>(Uri endpoint,
		                                                                                Action<T> messageConstructionAction)
			where T : IMessage
		{
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToSendEndpoint<T>(Uri endpoint) where T : IMessage
		{
			return ExpectNotToSendEndpoint<T>(endpoint, null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToSendEndpoint<T>(Uri endpoint,
		                                                                                   Action<T> messageConstructionAction)
			where T : IMessage
		{
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectReplyWith<T>() where T : IMessage
		{
			return ExpectReplyWith<T>(null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectToReplyAction<T, TStateMachine>(Container,
			                                                                    StateMachine,
			                                                                    MockServiceBus,
			                                                                    ConsumedMessage,
			                                                                    messageConstructionAction)
				.CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectNotToReplyWith<T>() where T : IMessage
		{
			return ExpectNotToReplyWith<T>(null);
		}

		public IStateMachineTestScenario<TStateMachine> ExpectNotToReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectNotToReplyAction<T, TStateMachine>(Container,
			                                                                       StateMachine,
			                                                                       MockServiceBus,
			                                                                       ConsumedMessage,
			                                                                       messageConstructionAction)
				.CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToRequestTimeout<T>(TimeSpan delayDuration)
			where T : IMessage
		{
			return ExpectToRequestTimeout<T>(delayDuration, null);
		}

		public virtual IStateMachineTestScenario<TStateMachine> ExpectToRequestTimeout<T>(TimeSpan delayDuration,
		                                                                                  Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectToRequestTimeoutAction<T, TStateMachine>(Container,
			                                                                             StateMachine,
			                                                                             MockServiceBus,
			                                                                             delayDuration,
			                                                                             ConsumedMessage,
			                                                                             messageConstructionAction).
				CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public IStateMachineTestScenario<TStateMachine> ExpectNotToDelay<T>(TimeSpan delayDuration) where T : IMessage
		{
			return ExpectNotToDelay<T>(delayDuration, null);
		}

		public IStateMachineTestScenario<TStateMachine> ExpectNotToDelay<T>(TimeSpan delayDuration,
		                                                                    Action<T> messageConstructionAction)
			where T : IMessage
		{
			Action<IMessage> verify = new ExpectNotToDelayAction<T, TStateMachine>(Container,
			                                                                       StateMachine,
			                                                                       MockServiceBus,
			                                                                       delayDuration,
			                                                                       ConsumedMessage,
			                                                                       messageConstructionAction).CreateExpectation();
			_verifiables.Add(verify);
			return this;
		}

		public IStateMachineTestScenario<TStateMachine> ExpectToTransitionToState(
			Expression<Func<TStateMachine, State>> expectedState)
		{
			var expected = expectedState.Compile().Invoke(StateMachine);

			Action checkForTransitionAction = () =>
			                                  	{
			                                  		if (expected == null)
			                                  			throw new StateMachineStateTransitionInvocationException(StateMachine.GetType(),
			                                  			                                                         StateMachine.
			                                  			                                                         	CurrentMessage.GetType
			                                  			                                                         	(),
			                                  			                                                         "null",
			                                  			                                                         StateMachine.
			                                  			                                                         	CurrentState.Name);

			                                  		if (StateMachine.CurrentState != null)
			                                  		{
			                                  			if (StateMachine.CurrentState != expected)
			                                  				throw new StateMachineStateTransitionInvocationException(
			                                  					StateMachine.GetType(),
			                                  					StateMachine.CurrentMessage.GetType(),
			                                  					expected.Name,
			                                  					StateMachine.CurrentState.Name);
			                                  		}
			                                  	};

			_postconditions.Add(checkForTransitionAction);
			return this;
		}

		public IStateMachineTestScenario<TStateMachine> ExpectToComplete()
		{
			Action checkForCompletionAction = () =>
			                                  	{
			                                  		if (StateMachine.IsCompleted == false)
			                                  			throw new StateMachineCompletionInvocationException(StateMachine.GetType(),
			                                  			                                                    StateMachine.CurrentMessage.
			                                  			                                                    	GetType());
			                                  	};

			_postconditions.Add(checkForCompletionAction);
			return this;
		}

		public IStateMachineTestScenario<TStateMachine> ExpectNotToComplete()
		{
			Action notCompleteAction = () =>
			                           	{
			                           		if (StateMachine.IsCompleted)
			                           			throw new StateMachineNonCompletionInvocationException(StateMachine.GetType(),
			                           			                                                       StateMachine.CurrentMessage.
			                           			                                                       	GetType());
			                           	};

			_postconditions.Add(notCompleteAction);
			return this;
		}

		public IStateMachineTestScenario<TStateMachine> SetAssertOn<TStateMachineData>(
			Expression<Func<TStateMachineData, bool>> expectation,
			string expectationMessage = "")
			where TStateMachineData : class, IStateMachineData
		{
			Action stateMachineDataCondition = () =>
			                                   	{
			                                   		try
			                                   		{
			                                   			TStateMachineData stateMachineData = default(TStateMachineData);
			                                   			GetStateMachineData(out stateMachineData);

			                                   			if (stateMachineData == null)
			                                   				throw new Exception(
			                                   					string.Format("The data for state machine '{0}' could not be retrieved.",
			                                   					              StateMachine.GetType().Name));

			                                   			if (expectation.Compile().Invoke(stateMachineData) == false)
			                                   			{
			                                   				if (string.IsNullOrEmpty(expectationMessage) == false)
			                                   					throw new Exception(expectationMessage);
			                                   				else
			                                   				{
			                                   					throw new Exception(
			                                   						string.Format("The data for state machine '{0}' failed assert condition",
			                                   						              StateMachine.GetType().Name));
			                                   				}
			                                   			}
			                                   		}
			                                   		catch (Exception exception)
			                                   		{
			                                   			throw new StateMachineDataExpectationException(exception.Message);
			                                   		}
			                                   	};

			_postconditions.Add(stateMachineDataCondition);
			return this;
		}

		public IStateMachineTestScenario<TStateMachine> Verify()
		{
			CheckForExternallyConfigurationForConsume();

			if (ConsumeMessageAction != null)
			{
				ConsumeMessageAction();
				Console.WriteLine(string.Format("Finished 'When' invocation for message '{0}'.",
				                                TryGetImplmentationFromProxiedMessage(ConsumedMessage).Name));
			}

			_verifiables.ForEach(verify => { if (verify != null) verify(ConsumedMessage); });
			_postconditions.ForEach(pc => pc());
			return this;
		}

		#endregion

		public IStateMachineTestScenario<TStateMachine> ExpectToNotTransitionToState(
			Expression<Func<TStateMachine, State>> expectedState)
		{
			var expected = expectedState.Compile().Invoke(StateMachine);

			Action checkForTransitionAction = () =>
			                                  	{
			                                  		if (expected == null)
			                                  			throw new StateMachineStateNonTransitionInvocationException(
			                                  				StateMachine.GetType(),
			                                  				StateMachine.CurrentMessage.GetType(),
			                                  				"null",
			                                  				StateMachine.CurrentState.Name);

			                                  		if (StateMachine.CurrentState != null)
			                                  		{
			                                  			if (StateMachine.CurrentState == expected)
			                                  				throw new StateMachineStateNonTransitionInvocationException(
			                                  					StateMachine.GetType(),
			                                  					StateMachine.CurrentMessage.GetType(),
			                                  					expected.Name,
			                                  					StateMachine.CurrentState.Name);
			                                  		}
			                                  	};

			_postconditions.Add(checkForTransitionAction);
			return this;
		}

		private void CheckForExternallyConfigurationForConsume()
		{
			if (ExternalConstructMessageAction != null)
			{
				IMessage message = ExternalConstructMessageAction();

				if (message != null)
				{
					ConsumedMessage = message;
				}
			}

			if (ConsumedMessage == null)
				throw new InvalidOperationException("There was not a message that could be " +
				                                    "constructed to pass into the state machine " +
				                                    "for the expectations to be verified.");
			else
			{
				// need to re-construct the consume action since the message was dyanimcally generated:
				ConsumeMessageAction = () =>
				                       	{
				                       		var dispatcher = Container.Resolve<ISagaStateMachineMessageDispatcher>();
				                       		dispatcher.Dispatch(MockServiceBus, StateMachine,
				                       		                    new Envelope(ConsumedMessage));
				                       	};
			}
		}

		private static Type TryGetImplmentationFromProxiedMessage(IMessage message)
		{
			Type result = message.GetType();

			if (message.GetType().Name.Contains("Proxy"))
			{
				// parent interface for proxied message:
				result = message.GetType().GetInterfaces()[0];
			}

			return result;
		}

		/// <summary>
		/// This will extract the current localized data of the state machine for inspection (only avaliable afer call to <seealso cref="Verify"/>)
		/// </summary>
		/// <typeparam name="TStateMachineData">Type representing the state machine data</typeparam>
		/// <returns></returns>
		private void GetStateMachineData<TStateMachineData>(out TStateMachineData stateMachineData)
			where TStateMachineData : class, IStateMachineData
		{
			stateMachineData = default(TStateMachineData);

			if (StateMachine != null)
			{
				var property = StateMachine.GetType().GetProperty("Data");

				if (property != null)
				{
					stateMachineData = property.GetValue(StateMachine, null) as TStateMachineData;
				}
			}
		}
	}
}