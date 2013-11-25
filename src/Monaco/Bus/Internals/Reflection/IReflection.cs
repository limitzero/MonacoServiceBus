using System;
using System.Collections.Generic;
using System.Reflection;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.StateMachine;
using Monaco.StateMachine.Roles;

namespace Monaco.Bus.Internals.Reflection
{
	/// <summary>
	/// Contract for internally based reflection helper functions.
	/// </summary>
	public interface IReflection
	{
		/// <summary>
		/// This will bulid a new instance of the object by type reference
		/// </summary>
		/// <param name="currentType">The type to create</param>
		/// <returns></returns>
		object BuildInstance(Type currentType);

		/// <summary>
		/// This will build a new instance of the object by the fully qualified type name.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		object BuildInstance(string typeName);

		void SetProperty(object theObject, string propertyName, object value);

		TData GetProperty<TData>(object consumer, string propetyName);

		/// <summary>
		/// This will search the current executable directory for the type specified
		/// and return the type instance.
		/// </summary>
		/// <param name="typeName">The fully qualified type to search for.</param>
		/// <returns></returns>
		Type FindType(string typeName);

		Type FindConcreteTypeForEndpointFactoryDefinition(string theFullyQualifiedType);

		Type FindConcreteTypeImplementingInterface(Type interfaceType, Assembly assemblyToScan);

		Type[] FindConcreteTypesImplementingInterface(Type interfaceType, Assembly assemblyToScan);

		object[] FindConcreteTypesImplementingInterfaceAndBuild(Type interfaceType, Assembly assemblyToScan);

		/// <summary>
		/// This will create an endpoint with the specified polling interval.
		/// </summary>
		/// <param name="factory"></param>
		/// <param name="endpointName"></param>
		/// <param name="endpointUri"></param>
		/// <returns></returns>
		object InvokeEndpointFactoryCreate(object factory, string endpointName, string endpointUri);

		/// <summary>
		/// This will call the "Remove" method on the saga data persister implementation
		/// to remove theinstance of a the saga data.
		/// </summary>
		/// <param name="sagaFinder">Current instance of the saga data finder</param>
		/// <param name="instanceId">Unique indentifier for the saga instance.</param>
		void InvokeRemoveForSagaDataRepository(object sagaFinder, Guid instanceId);

		/// <summary>
		/// This will call the "Save" method on the saga data persister implementation
		/// to save the saga data to the persistance store.
		/// </summary>
		/// <param name="sagaFinder">Current instance of the saga data finder</param>
		/// <param name="data">Saga data to save.</param>
		/// <returns>
		/// Saga data instance or null instance (implemented as object)
		/// </returns>
		void InvokeSaveForSagaDataRepository(object sagaFinder, IStateMachineData data);

		/// <summary>
		/// This will invoke the "HandleFault" method on the fault handler.
		/// </summary>
		/// <param name="faultHandler"></param>
		/// <param name="faultMessage"></param>
		/// <param name="envelope"></param>
		/// <param name="exception"></param>
		void InvokeHandleFaultForFaultHandler(object faultHandler, object faultMessage, IEnvelope envelope,
		                                      Exception exception = null);

		/// <summary>
		/// This will construct an assembly representing all of the interface-based only 
		/// message implementations primarily for the serialization engine.
		/// </summary>
		/// <param name="contracts">Collection of interfaces representing messages</param>
		/// <param name="registerInContainer">Flag to indicate whether or not to add these components to the container.</param>
		ICollection<Type> BuildProxyAssemblyForContracts(ICollection<Type> contracts, bool registerInContainer);

		/// <summary>
		/// This will build a concrete proxy object based on the interface contract.
		/// </summary>
		/// <typeparam name="TCONTRACT">Interface for building a concrete instance</typeparam>
		/// <returns>
		///  A concrete instance of the {TCONTRACT}.
		/// </returns>
		TCONTRACT BuildProxyFor<TCONTRACT>();

		/// <summary>
		/// This will invoke the FindAll() method on the indicated <seealso cref="IStateMachineDataRepository{TStateMachineData}"/>
		/// and return all of the active instances of the data for a given state machine for filtering.
		/// </summary>
		/// <param name="persister">Object representing the statemachine data repository.</param>
		/// <returns></returns>
		IEnumerable<IStateMachineData> InvokeStateMachineDataRepositoryFindAll(object persister);

		/// <summary>
		/// This will invoke the Find(Guid id) method on the indicated <seealso cref="ISagaDataFinder{TSaga, TMessage}"/>
		/// and return the instance of the saga data for the indicated saga instance by indentifier.
		/// </summary>
		/// <param name="persister">Object representing the saga data finder.</param>
		/// <param name="correlationId">Identifier of the state machine data instance for retreival</param>
		/// <returns></returns>
		IStateMachineData InvokeStateMachineDataRepositoryFindById(object persister, Guid correlationId);

		/// <summary>
		/// This will invoke the Find(Guid id) method on the indicated <seealso cref="ISagaDataFinder{TSaga, TMessage}"/>
		/// and return the instance of the saga data for the indicated saga instance by indentifier.
		/// </summary>
		/// <param name="persister">Object representing the saga data finder.</param>
		/// <param name="message">Identifier of the state machine data instance for retreival</param>
		/// <returns></returns>
		IStateMachineData InvokeStateMachineDataRepositoryFindByMessage(object persister, IMessage message);

		/// <summary>
		/// This will remove the current state machine data from the persistance store.
		/// </summary>
		/// <param name="persister">Instance of the state machine data repository</param>
		/// <param name="stateMachineData">Instance of state machine data to remove</param>
		void InvokeStateMachineDataRepositoryRemove(object persister, IStateMachineData stateMachineData);

		/// <summary>
		/// This will persist the current state machine data to the persistance store.
		/// </summary>
		/// <param name="persister">Instance of the state machine data repository</param>
		/// <param name="stateMachineData">Instance of state machine data to persist</param>
		void InvokeStateMachineDataRepositorySave(object persister, IStateMachineData stateMachineData);

		/// <summary>
		/// This will start the merge process on state machine data should the current version 
		/// of the data be greater than the retrived version from the persistance store.
		/// </summary>
		/// <param name="merger"></param>
		/// <param name="currentStateMachineData"></param>
		/// <param name="retreivedStateMachineData"></param>
		/// <param name="sagaMessage"></param>
		/// <returns></returns>
		IStateMachineData InvokeStateMachineDataMerge(object merger, 
			IStateMachineData currentStateMachineData,
		    IStateMachineData retreivedStateMachineData, 
			IMessage sagaMessage);

		/// <summary>
		/// This will allow for a message to be created either from concrete instance or if the message
		/// is an interface a proxy class will be created for the interface definition.
		/// </summary>
		/// <typeparam name="TMessage">Type to create a message for (i.e. concrete instance)</typeparam>
		/// <returns></returns>
		TMessage CreateMessage<TMessage>();
	}
}