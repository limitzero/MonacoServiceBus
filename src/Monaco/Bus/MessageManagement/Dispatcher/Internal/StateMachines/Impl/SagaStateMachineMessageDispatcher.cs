using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monaco.Bus.Internals;
using Monaco.Bus.Internals.Reflection;
using Monaco.Configuration;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Extensions;
using Monaco.StateMachine;
using Monaco.StateMachine.Internals.Impl;
using Monaco.StateMachine.Roles;

namespace Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines.Impl
{
	public class SagaStateMachineMessageDispatcher : ISagaStateMachineMessageDispatcher
	{
		private readonly IContainer container;
		private readonly ILogger logger;
		private readonly IReflection reflection;
		private string endpoint;
		private bool isMocked;

		public SagaStateMachineMessageDispatcher(IContainer container,
		                                         ILogger logger,
		                                         IReflection reflection)
		{
			this.container = container;
			this.logger = logger;
			this.reflection = reflection;
		}

		#region ISagaStateMachineMessageDispatcher Members

		public void Dispatch(IServiceBus bus, IConsumer consumer, IEnvelope envelope)
		{
			foreach (var message in envelope.Body.Payload)
			{
				var newEnvelope = envelope.Clone(message);
				this.DispatchInternal(bus, consumer, newEnvelope,  message as IMessage);
			}
		}

		#endregion

		private void DispatchInternal(IServiceBus bus, IConsumer consumer, IEnvelope envelope, IMessage message)
		{
			var sagaMessage = message;
			var stateMachine = consumer as SagaStateMachine;

			if (stateMachine == null)
				throw new Exception("The consumer passed to the saga state machine dispatcher must be derived from "
									+ string.Format("{0}<..>", typeof(SagaStateMachine).Name));

			if (sagaMessage == null)
				throw new Exception(
					string.Format("The message '{0}' passed into the state machine '{1}' must be derived from '{2}'.",
								  TryGetImplmentationFromProxiedMessage(sagaMessage).Name,
								  consumer.GetType().Name,
								  typeof(IMessage).FullName));

			CheckForSagaMessageConformingToContracts(stateMachine, sagaMessage);

			// checked for the saga state machine to be in unit test or "mocked" state:
			this.isMocked = bus.GetType().FullName.ToLower().Contains("proxy");
			this.endpoint = this.isMocked == false ? bus.Endpoint.EndpointUri.ToString() : "<mocked.endpoint.uri>";

			// define the state machine with the current instance of the service bus:
			TrySetServiceBusOnStateMachine(stateMachine, bus);

			logger.LogDebugMessage(string.Format("Start: dispatching message '{0}' to saga state machine '{1}'.",
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

			logger.LogDebugMessage(string.Format("Complete: dispatching message '{0}' to saga state machine '{1}'.",
												  sagaMessage.GetImplementationFromProxy().FullName,
												  consumer.GetType().FullName));
		}

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
				IStateMachineData mergedStateMachineData;

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

			this.logger.LogInfoMessage(
				string.Format("State Machine Data Version Upgrade: Attempting to merge state machine data of '{0}' " +
				              "with previous version of '{1}' to current version of '{2}' for retrieved instance '{3}'.",
				              currentStateMachineData.GetType().Name,
				              retreivedStateMachineData.Version,
				              currentStateMachineData.Version, 
							  retreivedStateMachineData.Id));

			// reconcile the data versions by merge process:
			Type dataMergerType = typeof (IStateMachineDataMerger<,>).MakeGenericType(currentStateMachineData.GetType(),
			                                                                          message.GetImplementationFromProxy());

			try
			{
				object merger = container.Resolve(dataMergerType);
				mergedStateMachineData = reflection.InvokeStateMachineDataMerge(merger, currentStateMachineData,
				                                                                 retreivedStateMachineData, message);

				if (mergedStateMachineData != null)
				{
					success = true;

					// retain the instance identifier and upgrade the version on the merged data to the most recent (if successful):
					mergedStateMachineData.Id = currentStateMachineData.Id; 
					mergedStateMachineData.Version = currentStateMachineData.Version; 

					this.logger.LogInfoMessage(string.Format("State Machine Data Version Upgrade: State machine data '{0}' merged from " +
														 " previous version of '{1}' to current version of '{2}' for retrieved instance '{3}'.",
					                                     currentStateMachineData.GetType().Name,
					                                     retreivedStateMachineData.Version,
					                                     currentStateMachineData.Version, 
														 retreivedStateMachineData.Id));
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
		private static void TrySetServiceBusOnStateMachine(SagaStateMachine stateMachine, IServiceBus bus)
		{
			var busProperty = (from match in stateMachine.GetType().GetProperties()
			                   where match.PropertyType == typeof (IServiceBus)
			                   select match).FirstOrDefault();

			if (busProperty != null)
			{
				busProperty.SetValue(stateMachine, bus, null);
			}
		}

		private static void SetStateMachineDataForStateMachine(SagaStateMachine stateMachine, IStateMachineData stateMachineData)
		{
			PropertyInfo dataProperty = FindPropertyForStateMachineData(stateMachine);
			dataProperty.SetValue(stateMachine, stateMachineData, null);
		}

		private IStateMachineData FindStateMachineDataFromCurrentMessage(SagaStateMachine stateMachine, IMessage message)
		{
			IStateMachineData stateMachineData;

			// use the custom data finder as defined for custom searches and basic "find by id" search:
			if (TryFindStateMachineDataFromCorrelationMapping(message, stateMachine, out stateMachineData) == false)
			{
				if (TryFindStateMachineDataFromDataFinder(message, stateMachine, out stateMachineData) == false)
				{
					// could not find the data from finder...need to throw exception here!!!
					throw new Exception(
						string.Format(
							"No state machine data could be retreived for message '{0}' for state machine '{1}' via correlation or specialized data finders.",
							message.GetType().FullName,
							stateMachine.GetType().Name));
				}
			}

			return stateMachineData;
		}

		private bool TryFindStateMachineDataFromDataFinder(IMessage sagaMessage,
		                                                   SagaStateMachine stateMachine,
		                                                   out IStateMachineData stateMachineData)
		{
			bool success = false;
			stateMachineData = null;

			Type dataPropertyType = FindTypeForStateMachineData(stateMachine);
			Type messageType = TryGetImplmentationFromProxiedMessage(sagaMessage);

			Type finderType = typeof (IStateMachineDataFinder<,>).MakeGenericType(dataPropertyType, messageType);

			try
			{
				object finder = this.container.Resolve(finderType);
				stateMachineData = this.reflection.InvokeStateMachineDataRepositoryFindByMessage(finder, sagaMessage);

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
		                                                           SagaStateMachine stateMachine,
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
					object finder = this.container.Resolve(finderType);
					IEnumerable<IStateMachineData> stateMachineDataItems =
						this.reflection.InvokeStateMachineDataRepositoryFindAll(finder);

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
					// no data found for the predicate data search
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
		private static void CheckForSagaMessageConformingToContracts(SagaStateMachine stateMachine,
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

		private bool CanCreateNewInstanceOfStateMachineData(SagaStateMachine stateMachine, IMessage sagaMessage)
		{
			bool isNewStateMachineInstance = false;
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
					IStateMachineData stateMachineData;

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
				var data = this.reflection.BuildInstance(dataProperty.PropertyType) as IStateMachineData;

				// need to grab data for saga and sync the instance identifier
				// with the instance identifier of the saga (for the first time access):
				if (data != null)
				{
					data.Id = instanceId;
					this.reflection.SetProperty(stateMachine, "Data", data);
				}
			}
		}

		private void ConsumeMessage<TMessage>(SagaStateMachine stateMachine, TMessage message)
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
				this.logger.LogDebugMessage(string.Format("Current state of saga state machine '{0}' is '{1}'.",
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

			// check the message pre-condition before consuming (if stated):
			if (currentTriggerCondition.Condition.PreCondition != null)
			{
				var result = currentTriggerCondition.Condition.PreCondition.Compile().Invoke();

				if (result == false)
					throw new InvalidOperationException("The state machine could not execute the message actions due to a pre-condition failure.");
			}

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
				this.logger.LogDebugMessage(string.Format("Saga state machine '{0}' has transitioned from state '{1}' to state '{2}'.",
				                                      stateMachine.GetType().FullName,
				                                      previousState.Name,
				                                      currentState.Name));
			}
		}

		private static SagaStateMachineDefinedTriggerCondition FindTriggerConditionForInitialConverstationOnStateMachine(
			SagaStateMachine statemachine,
			IMessage sagaMessage)
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
			SagaStateMachine statemachine,
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
				                    select match).FirstOrDefault();
			}

			return triggerCondition;
		}

		/// <summary>
		/// This will persist the current contents of the data for the state machine.
		/// </summary>
		/// <param name="stateMachine"></param>
		private void PersistStateMachineData(SagaStateMachine stateMachine)
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
				persister = this.container.Resolve(persisterType);
			}
			catch (Exception exception)
			{
				throw new Exception(string.Format("No state machine data persistance was registered on the endpoint '{0}'. " +
				                                  "Please ensure that the state machine data persistance has been defined in the configuration file or endpoint " +
				                                  "configuration class and the concrete implementation is derived from " +
				                                  typeof (IStateMachineDataRepository<>),
				                                  endpoint), exception);
			}

			try
			{
				if (stateMachine.IsCompleted)
				{
					/* remove the instance data from the repository for the state machine */
					this.reflection.InvokeStateMachineDataRepositoryRemove(persister, stateMachineData);

					/*  need to clean up all timeouts for the state machine when it is completed (will have orphaned timeouts !!)*/
					var timeoutsRepository = this.container.Resolve<ITimeoutsRepository>();
					timeoutsRepository.RemoveRequestedTimeouts(stateMachineData.Id);
				}
				else
				{
					this.reflection.InvokeStateMachineDataRepositorySave(persister, stateMachineData);
				}
			}
			catch
			{
			}
		}

		private static PropertyInfo FindPropertyForStateMachineData(SagaStateMachine stateMachine)
		{
			var stateMachineDataProperty = (from match in stateMachine.GetType().GetProperties()
			                                where typeof (IStateMachineData).IsAssignableFrom(match.PropertyType)
			                                select match).FirstOrDefault();


			if (stateMachineDataProperty != null)
				return stateMachineDataProperty;

			return null;
		}

		private static Type FindTypeForStateMachineData(SagaStateMachine stateMachine)
		{
			Type stateMachineDataType = null;

			PropertyInfo property = FindPropertyForStateMachineData(stateMachine);

			if (property != null)
			{
				stateMachineDataType = property.PropertyType;
			}

			return stateMachineDataType;
		}

		private State TransitionToIndicatedState(SagaStateMachine stateMachine,
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

			var data = this.reflection.GetProperty<IStateMachineData>(stateMachine, "Data");

			if (data != null && currentState != null)
			{
				data.State = currentState.Name;
			}

			return currentState;
		}

		private static void CheckForCorrelation<TMessage>(SagaStateMachine stateMachine,
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