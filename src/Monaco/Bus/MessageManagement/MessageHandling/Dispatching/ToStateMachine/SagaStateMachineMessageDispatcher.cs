using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Reflection;
using Monaco.Extensibility.Logging;
using Monaco.Extensions;
using Monaco.StateMachine;
using Monaco.StateMachine.Internals.Impl;

namespace Monaco.Bus.MessageManagement.MessageHandling.Dispatching.ToStateMachine
{
	public class SagaStateMachineMessageDispatcher : ISagaStateMachineMessageDispatcher
	{
		private readonly IKernel _kernel;
		private readonly ILogger _logger;
		private readonly IReflection _reflection;
		private string _endpoint;
		private bool _isMocked;

		public SagaStateMachineMessageDispatcher(IKernel kernel,
		                                         ILogger logger,
		                                         IReflection reflection)
		{
			_kernel = kernel;
			_logger = logger;
			_reflection = reflection;
		}

		#region ISagaStateMachineMessageDispatcher Members

		public void Dispatch(IServiceBus bus, IConsumer consumer, IEnvelope envelope)
		{
			var sagaMessage = envelope.Body.Payload as IMessage;
			var stateMachine = consumer as StateMachine;

			if (stateMachine == null)
				throw new Exception("The consumer passed to the saga state machine dispatcher must be derived from "
				                    + string.Format("{0}<..>", typeof (StateMachine).Name));

			if (sagaMessage == null)
				throw new Exception(
					string.Format("The message '{0}' passed into the state machine '{1}' must be derived from '{2}'.",
					              TryGetImplmentationFromProxiedMessage(sagaMessage).Name,
					              consumer.GetType().Name,
					              typeof (IMessage).FullName));

			CheckForSagaMessageConformingToContracts(stateMachine, sagaMessage);

			// checked for the saga state machine to be in unit test or "mocked" state:
			_isMocked = bus.GetType().FullName.ToLower().Contains("proxy");
			_endpoint = _isMocked == false ? bus.Endpoint.EndpointUri.ToString() : "<mocked.endpoint.uri>";

			// define the state machine with the current instance of the service bus:
			TrySetServiceBusOnStateMachine(stateMachine, bus);

			_logger.LogDebugMessage(string.Format("Start: dispatching message '{0}' to saga state machine '{1}'.",
			                                      sagaMessage.GetImplementationFromProxy().FullName,
			                                      consumer.GetType().FullName));

			envelope.Header.RecordStage(consumer, sagaMessage, "Dispatch::" + stateMachine.GetType().FullName);

			// set the message that the state machine is handling:
			stateMachine.CurrentMessage = sagaMessage;

			SagaStateMachineDefinedTriggerCondition triggerCondition =
				FindTriggerConditionForConverationOnStateMachine(stateMachine, sagaMessage);

			if (CanCreateNewInstanceOfStateMachineData(stateMachine, sagaMessage))
			{
				Initialize(stateMachine);
				stateMachine.CurrentState = new Start();
			}
			else
			{
				// Restraints : The message passed in must contain the correlation identifier/property 
				// of the message used to start the state machine, this is the only way to enusure
				// that the same state machine data will be applied to the running instance of the 
				// state machine, or a custom state machine data finder must be created to pull the 
				// data if the correlation identifier is not found using the current message.

				// check the correlation on the current message to the state machine (only for subsequent conversations):
				if (triggerCondition != null)
				{
					CheckForCorrelation(stateMachine, sagaMessage, triggerCondition);
				}

				// next, get the persisted state machine data for the instance identifier on the message
				// and place the data on the state machine that was instantiated by the component container:
				var stateMachineData = FindStateMachineDataFromCurrentMessage(stateMachine, sagaMessage);
				SetStateMachineDataForStateMachine(stateMachine, stateMachineData);

				// next, check to see if the versions on the state machine data are different, 
				// if they are we will need to call the state machine data merge components 
				// to consolidate the data and pass to the running instance of the state machine:
				CheckForUpdateVersionOnStateMachineData(stateMachine, sagaMessage, stateMachineData);
			}

			// reset the message for the indicated trigger condition (if not there), before consuming message
			if (triggerCondition != null && triggerCondition.Message == null)
			{
				triggerCondition.Message = sagaMessage;
			}

			ConsumeMessage(stateMachine, sagaMessage);

			PersistStateMachineData(stateMachine);

			_logger.LogDebugMessage(string.Format("Complete: dispatching message '{0}' to saga state machine '{1}'.",
			                                      sagaMessage.GetImplementationFromProxy().FullName,
			                                      consumer.GetType().FullName));
		}

		#endregion

		/// <summary>
		/// This will check to see if the instance data version has been incremented since the 
		/// last invocation and will try to invoke the configured data merger (if available) to 
		/// reconcile the differences.
		/// </summary>
		/// <param name="stateMachine">Current state machine being invoked</param>
		/// <param name="message">Current message being passed to state machine</param>
		/// <param name="retreivedStateMachineData">Retrieved state machine data from repository</param>
		private void CheckForUpdateVersionOnStateMachineData(SagaStateMachine stateMachine,
		                                                     IMessage message, IStateMachineData retreivedStateMachineData)
		{
			Type stateMachinePropertyType = FindTypeForStateMachineData(stateMachine);
			var currentStateMachineData = Activator.CreateInstance(stateMachinePropertyType) as IStateMachineData;

			if (currentStateMachineData == null)
			{
				// nothing we can do here, use the current instance data and keep going:
				SetStateMachineDataForStateMachine(stateMachine, retreivedStateMachineData);
				return;
			}

			// this assumes that version information has been set in the constructor:
			if (currentStateMachineData.Version > retreivedStateMachineData.Version)
			{
				IStateMachineData mergedStateMachineData = null;

				if (TryMergeStateMachineData(message,
				                             currentStateMachineData, retreivedStateMachineData, out mergedStateMachineData))
				{
					SetStateMachineDataForStateMachine(stateMachine, mergedStateMachineData);
				}
			}
		}

		private bool TryMergeStateMachineData(
			IMessage message,
			IStateMachineData currentStateMachineData,
			IStateMachineData retreivedStateMachineData,
			out IStateMachineData mergedStateMachineData)
		{
			bool success = false;
			mergedStateMachineData = retreivedStateMachineData;

			_logger.LogInfoMessage(
				string.Format("State Machine Data Version Upgrade: Attempting to merging state machine data of '{0}' " +
				              "with previous version of '{1}' to current version of '{2}'.",
				              currentStateMachineData.GetType().Name,
				              retreivedStateMachineData.Version,
				              currentStateMachineData.Version));

			// reconcile the data versions by merge process:
			Type dataMergerType = typeof (IStateMachineDataMerger<,>).MakeGenericType(currentStateMachineData.GetType(),
			                                                                          message.GetType());

			try
			{
				object merger = _kernel.Resolve(dataMergerType);
				mergedStateMachineData = _reflection.InvokeStateMachineDataMerge(merger, currentStateMachineData,
				                                                                 retreivedStateMachineData, message);

				if (mergedStateMachineData != null)
				{
					success = true;

					_logger.LogInfoMessage(string.Format("State Machine Data Version Upgrade: State machine data '{0}' merged from " +
					                                     " previous version of '{1}' to current version of '{2}'.",
					                                     currentStateMachineData.GetType().Name,
					                                     retreivedStateMachineData.Version,
					                                     currentStateMachineData.Version));
				}
				else
				{
					mergedStateMachineData = retreivedStateMachineData;
				}
			}
			catch
			{
				// no data merger defined..use retrieved state
				mergedStateMachineData = retreivedStateMachineData;
			}

			return success;
		}

		/// <summary>
		/// This will set the service bus instance on the state machine for message dispatching via the bus.
		/// </summary>
		/// <param name="stateMachine"></param>
		/// <param name="bus"></param>
		private static void TrySetServiceBusOnStateMachine(StateMachine stateMachine, IServiceBus bus)
		{
			var busProperty = (from match in stateMachine.GetType().GetProperties()
			                   where match.PropertyType == typeof (IServiceBus)
			                   select match).FirstOrDefault();

			if (busProperty != null)
			{
				busProperty.SetValue(stateMachine, bus, null);
			}
		}

		private void SetStateMachineDataForStateMachine(StateMachine stateMachine, IStateMachineData stateMachineData)
		{
			PropertyInfo dataProperty = FindPropertyForStateMachineData(stateMachine);
			dataProperty.SetValue(stateMachine, stateMachineData, null);
		}

		private IStateMachineData FindStateMachineDataFromCurrentMessage(StateMachine stateMachine, IMessage message)
		{
			IStateMachineData stateMachineData = null;

			// use the custom data finder as defined for custom searches and basic "find by id" search:
			if (TryFindStateMachineDataFromCorrelationMapping(message, stateMachine, out stateMachineData) == false)
			{
				if (TryFindStateMachineDataFromDataFinder(message, stateMachine, out stateMachineData) == false)
				{
					// could not find the data from finder...need to throw exception here!!!
					throw new Exception(
						string.Format(
							"No state machine data could be retreived for message '{0}' for state machine '{1}'.",
							message.GetType().FullName,
							stateMachine.GetType().Name));
				}
			}

			return stateMachineData;
		}

		private bool TryFindStateMachineDataFromDataFinder(IMessage sagaMessage,
		                                                   StateMachine stateMachine,
		                                                   out IStateMachineData stateMachineData)
		{
			bool success = false;
			stateMachineData = null;

			Type dataPropertyType = FindTypeForStateMachineData(stateMachine);
			Type messageType = TryGetImplmentationFromProxiedMessage(sagaMessage);

			Type finderType = typeof (IStateMachineDataFinder<,>).MakeGenericType(dataPropertyType, messageType);

			try
			{
				object finder = _kernel.Resolve(finderType);
				stateMachineData = _reflection.InvokeStateMachineDataRepositoryFindByMessage(finder, sagaMessage);

				if (stateMachineData != null)
					success = true;
			}
			catch
			{
				// no data found for the find by message search...
			}

			return success;
		}

		private bool TryFindStateMachineDataFromCorrelationMapping(IMessage message,
		                                                           StateMachine stateMachine,
		                                                           out IStateMachineData stateMachineData)
		{
			bool success = false;
			stateMachineData = null;

			Type messageType = TryGetImplmentationFromProxiedMessage(message);

			var correlation = (from expression in stateMachine.StateMachineDataToMessageCorrelationExpressions
			                   where
			                   	expression.StateMachineMessage.IsAssignableFrom(messageType)
			                   select expression).FirstOrDefault();

			// match the data based on the predicate function from the data to the message:
			if (correlation != null)
			{
				Type dataPropertyType = FindTypeForStateMachineData(stateMachine);

				Type finderType = typeof (IStateMachineDataRepository<>).MakeGenericType(dataPropertyType);

				try
				{
					object finder = _kernel.Resolve(finderType);
					IEnumerable<IStateMachineData> stateMachineDataItems =
						_reflection.InvokeStateMachineDataRepositoryFindAll(finder);

					if (stateMachineDataItems != null)
					{
						stateMachineData = (from data in stateMachineDataItems
						                    where correlation.IsMatch(data, message) == true
						                    select data).FirstOrDefault();

						if (stateMachineData != null)
							success = true;
					}
				}
				catch
				{
					// no data found for the find by message search...
				}
			}

			return success;
		}

		private static Type TryGetImplmentationFromProxiedMessage(IMessage sagaMessage)
		{
			Type result = sagaMessage.GetType();

			if (sagaMessage.GetType().Name.Contains("Proxy"))
			{
				// parent interface for proxied message:
				result = sagaMessage.GetType().GetInterfaces()[0];
			}

			return result;
		}

		/// <summary>
		/// This will make sure that the message passed into the saga is stated in the interface contracts of 
		/// <seealso cref="StartedBy{T}"/> and <seealso cref="OrchestratedBy{T}"/>
		/// </summary>
		/// <param name="stateMachine"></param>
		/// <param name="sagaMessage"></param>
		private static void CheckForSagaMessageConformingToContracts(StateMachine stateMachine,
		                                                             IMessage sagaMessage)
		{
			Type currentMessageType = TryGetImplmentationFromProxiedMessage(sagaMessage);

			// consuming interfaces on statemachine:
			var interfaces = (from match in stateMachine.GetType().GetInterfaces()
			                  where typeof (IConsumer).IsAssignableFrom(match)
			                        && match.IsGenericType == true
			                  select match).ToList().Distinct();

			// search all interface generic arguments to see if the message can be assigned to it:
			var canAssignMessageToStateMachine = (from match in interfaces
			                                      let messageType = match.GetGenericArguments()[0]
			                                      where messageType.IsAssignableFrom(currentMessageType)
			                                      select true).First();

			if (canAssignMessageToStateMachine == false)
				throw new InvalidOperationException(
					string.Format("The message '{0}' could not be defined in a role for '{1}' or '{2}' for state machine '{3}'. " +
					              "Please marke the message for consumption by using either of these interfaces denoting these roles.",
					              TryGetImplmentationFromProxiedMessage(sagaMessage).Name,
					              typeof (StartedBy<>).Name,
					              typeof (OrchestratedBy<>).Name,
					              stateMachine.GetType().Name));
		}

		private bool CanCreateNewInstanceOfStateMachineData(StateMachine stateMachine, IMessage sagaMessage)
		{
			bool isNewStateMachineInstance = false;
			IStateMachineData stateMachineData = null;
			Type currentMessageType = TryGetImplmentationFromProxiedMessage(sagaMessage);
			Type startStateMachineType = typeof (StartedBy<>).MakeGenericType(currentMessageType);

			stateMachine.ConfigureHowToFindStateMachineInstanceDataFromMessages();

			if (startStateMachineType.IsAssignableFrom(stateMachine.GetType()))
			{
				//if (stateMachine.CurrentInitializationMappingConfigurationExpression != null)
				//{
				//    // needs to fail the mapping initialization and no state machine data resident for message to be "started":
				//    isNewStateMachineInstance =
				//        stateMachine.CurrentInitializationMappingConfigurationExpression() &&
				//        (TryFindStateMachineDataFromDataFinder(sagaMessage, stateMachine, out stateMachineData) == false) &&
				//        (TryFindStateMachineDataFromDataFinderById(sagaMessage, stateMachine, out stateMachineData) == false);
				//}

				if (stateMachine.StateMachineDataToMessageCorrelationExpressions.Count > 0)
				{
					isNewStateMachineInstance =
						TryFindStateMachineDataFromCorrelationMapping(sagaMessage, stateMachine, out stateMachineData) == false &&
						(TryFindStateMachineDataFromDataFinder(sagaMessage, stateMachine, out stateMachineData) == false);
				}
			}

			return isNewStateMachineInstance;
		}

		private void Initialize(IConsumer stateMachine)
		{
			Guid instanceId = CombGuid.NewGuid();
			((IStateMachine) stateMachine).InstanceId = instanceId;

			PropertyInfo dataProperty = (from match in stateMachine.GetType().GetProperties()
			                             where typeof (IStateMachineData).IsAssignableFrom(match.PropertyType)
			                             select match).FirstOrDefault();

			// create the new instance of the saga data and place it on the current saga state machine:
			if (dataProperty != null)
			{
				var data = _reflection.BuildInstance(dataProperty.PropertyType) as IStateMachineData;

				// need to grab data for saga and sync the instance identifier
				// with the instance identifier of the saga (for the first time access):
				if (data != null)
				{
					data.CorrelationId = instanceId;
					_reflection.SetProperty(stateMachine, "Data", data);
				}
			}
		}

		private void ConsumeMessage<TMessage>(StateMachine stateMachine, TMessage message)
			where TMessage : IMessage
		{
			if (stateMachine == null) return;

			// define all of the trigger conditions for processing the current message:
			if (stateMachine.TriggerConditions.Count == 0)
			{
				stateMachine.Define();
			}

			State currentState = stateMachine.CurrentState;

			if (currentState != null)
				_logger.LogDebugMessage(string.Format("Current state of saga state machine '{0}' is '{1}'.",
				                                      stateMachine.GetType().FullName, currentState.Name));

			// set the current message that the state machine is consuming:
			stateMachine.CurrentMessage = message;

			// re-adjust the state of the machine if it has been forcibly set (the define method
			// pushes it to the "Start" state if not retrived from the persistance store):
			if (currentState != null)
			{
				if (currentState.Name != new Start().Name)
				{
					stateMachine.CurrentState = currentState;
				}
			}

			SagaStateMachineDefinedTriggerCondition currentTriggerCondition =
				FindTriggerConditionForConverationOnStateMachine(stateMachine, message);

			// nothing we can do here, no state or message matching the trigger condition can be found
			// (most likely the action was carried out on the Consume<T>(T message) method):
			if (currentTriggerCondition == null) return;

			State previousState = stateMachine.CurrentState;

			// always skip the "When" event since it is fired above on consuming the message
			// and execute the subsquent actions on the current state for the state machine:

			// first, transition to the state indicated then execute the actions 
			// so that the state machine will always be in the proper state when
			// the message actions are executed (if we do it after, the state machine
			// will be in an inconsistent state):
			currentState = TransitionToIndicatedState(stateMachine, currentState, currentTriggerCondition);

			// consume the message on the state machine first (this is the "When" part):
			MethodInfo method = new MessageToMethodMapper().Map(stateMachine, message);
			new MessageMethodInvoker().Invoke(stateMachine, method, message);

			// second, execute all of the supporting actions except the "When", "Correlate", and "TransitionTo":
			currentTriggerCondition.Condition.Recorder.Play(message);

			// indicate the change in the state machine:
			if (currentState != null &&
			    previousState != null &&
			    previousState != currentState)
			{
				_logger.LogDebugMessage(string.Format("Saga state machine '{0}' has transitioned from state '{1}' to state '{2}'.",
				                                      stateMachine.GetType().FullName,
				                                      previousState.Name,
				                                      currentState.Name));
			}
		}

		private static SagaStateMachineDefinedTriggerCondition FindTriggerConditionForInitialConverstationOnStateMachine(
			StateMachine statemachine,
			ISagaMessage sagaMessage)
		{
			SagaStateMachineDefinedTriggerCondition triggerCondition = null;

			if (statemachine.TriggerConditions != null)
			{
				// check the "Initially" condition for the current message:
				triggerCondition = (from match in statemachine.TriggerConditions
				                    let theActions = match.Condition.MessageActions
				                    where (match.Stage == SagaStateMachineStageType.Initially)
				                          && match.Message.GetType() == sagaMessage.GetType()
				                    select match).FirstOrDefault();
			}

			return triggerCondition;
		}

		private static SagaStateMachineDefinedTriggerCondition FindTriggerConditionForConverationOnStateMachine(
			StateMachine statemachine,
			IMessage sagaMessage)
		{
			SagaStateMachineDefinedTriggerCondition triggerCondition = null;

			if (statemachine.TriggerConditions != null)
			{
				// check the "Initially", "While" or "Also" conditions for the current message:
				triggerCondition = (from match in statemachine.TriggerConditions
				                    let theActions = match.Condition.MessageActions
				                    let actionMessageType = TryGetImplmentationFromProxiedMessage(match.Message)
				                    let currentMessageType = TryGetImplmentationFromProxiedMessage(sagaMessage)
				                    where (
				                          	match.Stage == SagaStateMachineStageType.Initially ||
				                          	match.Stage == SagaStateMachineStageType.While ||
				                          	match.Stage == SagaStateMachineStageType.Also)
				                          && actionMessageType == currentMessageType
				                    //&& match.Message.GetType() == sagaMessage.GetType()								
				                    select match).FirstOrDefault();
			}

			return triggerCondition;
		}

		/// <summary>
		/// This will persist the current contents of the data for the state machine.
		/// </summary>
		/// <param name="stateMachine"></param>
		private void PersistStateMachineData(StateMachine stateMachine)
		{
			if (stateMachine == null) return;

			PropertyInfo stateMachineDataProperty = FindPropertyForStateMachineData(stateMachine);
			Type stateMachineDataPropertyType = stateMachineDataProperty.PropertyType;
			var stateMachineData = stateMachineDataProperty.GetValue(stateMachine, null) as IStateMachineData;

			object persister = null;

			try
			{
				Type persisterType = typeof (IStateMachineDataRepository<>)
					.MakeGenericType(stateMachineDataPropertyType);
				persister = _kernel.Resolve(persisterType);
			}
			catch (Exception exception)
			{
				throw new Exception(string.Format("No state machine data persistance was registered on the endpoint '{0}'. " +
				                                  "Please ensure that the state machine data persistance has been defined in the configuration file or endpoint " +
				                                  "configuration class and the concrete implementation is derived from " +
				                                  typeof (IStateMachineDataRepository<>),
				                                  _endpoint), exception);
			}

			try
			{
				if (stateMachine.IsCompleted)
				{
					_reflection.InvokeStateMachineDataRepositoryRemove(persister, stateMachineData);
				}
				else
				{
					_reflection.InvokeStateMachineDataRepositorySave(persister, stateMachineData);
				}
			}
			catch
			{
			}
		}

		private PropertyInfo FindPropertyForStateMachineData(StateMachine stateMachine)
		{
			var stateMachineDataProperty = (from match in stateMachine.GetType().GetProperties()
			                                where typeof (IStateMachineData).IsAssignableFrom(match.PropertyType)
			                                select match).FirstOrDefault();


			if (stateMachineDataProperty != null)
				return stateMachineDataProperty;

			return null;
		}

		private Type FindTypeForStateMachineData(SagaStateMachine stateMachine)
		{
			Type stateMachineDataType = null;

			PropertyInfo property = FindPropertyForStateMachineData(stateMachine);

			if (property != null)
			{
				stateMachineDataType = property.PropertyType;
			}

			return stateMachineDataType;
		}

		private State TransitionToIndicatedState(StateMachine stateMachine,
		                                         State currentState,
		                                         SagaStateMachineDefinedTriggerCondition definedTriggerCondition)
		{
			var transition = (from match in definedTriggerCondition.Condition.MessageActions
			                  where match.ActionType == SagaStateMachineMessageActionType.Transition
			                  select match).FirstOrDefault();

			if (transition != null)
			{
				currentState = transition.State;
				stateMachine.MarkTransitionTo(transition.State);
			}

			var data = _reflection.GetProperty<IStateMachineData>(stateMachine, "Data");

			if (data != null && currentState != null)
			{
				data.State = currentState.Name;
			}

			return currentState;
		}

		private static void CheckForCorrelation<TMessage>(StateMachine stateMachine,
		                                                  TMessage message,
		                                                  SagaStateMachineDefinedTriggerCondition triggerCondition)
			where TMessage : IMessage
		{
			var actions = triggerCondition.Condition.MessageActions;

			if (actions != null)
			{
				MessageAction correlation = null;

				try
				{
					correlation = actions.FirstOrDefault((match) =>
					                                     match.ActionType == SagaStateMachineMessageActionType.Correlate
					                                     & match.Message.GetType() == message.GetType());
				}
				catch
				{
				}

				if (correlation != null)
				{
					correlation.Action(message);
				}
			}
		}
	}
}