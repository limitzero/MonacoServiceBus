using System;
using System.Reflection;
using System.Transactions;
using Monaco.Bus.Internals;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Configuration.Registration;

namespace Monaco.Configuration
{
	public interface IEndpointConfiguration
	{
		/// <summary>
		/// This will configure a message to have one or more fault handlers in the event that it can not be 
		/// processed by the message consumer. The fault handlers will run in the sequence defined.
		/// </summary>
		/// <param name="configuration">Fault handler configuration for a message</param>
		IEndpointConfiguration ConfigureMessageFaultHandlerChain(Func<FaultHandlerConfiguration, FaultHandlerConfiguration> configuration);

		/// <summary>
		/// This will configure a message to be processed by a series of message consumers in a defined order.
		/// </summary>
		/// <param name="configuration">Handler configuration for a a message</param>
		IEndpointConfiguration ConfigureMessageConsumerHandlingChain(Func<HandlerConfiguration, HandlerConfiguration> configuration);

		/// <summary>
		/// This will attempt to enforce transactional behavior 
		/// on the messaging transport (if supported). For items 
		/// that can participate with MSDTC, the transaction scope
		/// will be used, for others, they will receive an event when 
		/// the message is processed (without error) and can determine
		/// the disposal rules for the message at that point.
		/// </summary>
		/// <param name="supportsTransactions"></param>
		IEndpointConfiguration SupportsTransactions(bool supportsTransactions);

			/// <summary>
		/// This will allow the service bus to set the isolation level of the transaction
		/// when accessing transactional resources (only applied if the transport sets
		/// the "IsTransactional" flag to "true").
		/// </summary>
		/// <param name="level"></param>
		IEndpointConfiguration SetTransactionIsolationLevel(IsolationLevel level);

		/// <summary>
		/// This will map a series of messages for a consumer to the local bus endpoint and 
		/// register the consumer in the container.
		/// </summary>
		/// <typeparam name="TConsumer">Consumer that will be connected to the bus endpoint for message receipt.</typeparam>
		IEndpointConfiguration MapMessages<TConsumer>() where TConsumer : IConsumer;

		/// <summary>
		///  This will map a series of messages to a remote endpoint from an assembly.
		/// </summary>
		/// <param name="messageAssembly">Assembly name of the messages that will be mapped to a remote endpoint</param>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		IEndpointConfiguration MapMessages(string messageAssembly, Uri endpoint);

		/// <summary>
		///  This will map a series of known messages to a remote endpoint.
		/// </summary>
		/// <param name="messages">Collection of messages to map to a remote endpoint.</param>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		IEndpointConfiguration MapMessages(Uri endpoint, params IMessage[] messages);

		/// <summary>
		/// This will map a series of messages for a consumer to a remote endpoint.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer for the messages.</typeparam>
		/// <param name="endpoint">Uri corresponding to the remote endpoint</param>
		/// <param name="register">Flag to indicate whether or not to automatically register the consumer in the container.</param>
		IEndpointConfiguration MapMessage<TConsumer>(Uri endpoint, bool register = false) where TConsumer : IConsumer;

		/// <summary>
		///  This will map a single  known message to a remote endpoint.
		/// </summary>
		/// <typeparam name="TMessage">Type of the messge to map to an endpoint</typeparam>
		/// <param name="endpoint">Uri corresponding a remote endpoint</param>
		IEndpointConfiguration MapMessage<TMessage>(Uri endpoint) where TMessage : IMessage;

		/// <summary>
		/// This will map all message consumer messages to the local endpoint and register the 
		/// consumers in the underlying container.
		/// </summary>
		IEndpointConfiguration MapAll(Assembly assembly);

		/// <summary>
		/// This will register a message producing component as a 
		/// time-based task that will send a message out to the 
		/// service bus for others to consume.
		/// </summary>
		/// <typeparam name="TProducer">Component that produces a message on a given interval</typeparam>
		/// <param name="name">The name of the task</param>
		/// <param name="span">The interval that the task should run expressed as hours:minutes:seconds</param>
		IEndpointConfiguration MapTask<TProducer>(string name, TimeSpan span) where TProducer : class, IProducer;

		/// <summary>
		/// This will register a message producing component as a 
		/// time-based task that will send a message out to the 
		/// service bus for others to consume.
		/// </summary>
		/// <typeparam name="TProducer">Component that produces a message on a given interval</typeparam>
		/// <param name="name">The name of the task</param>
		/// <param name="span">The interval that the task should run expressed as hours:minutes:seconds</param>
		/// <param name="haltOnError">Flag to determine if the task should be halted if it generates an exception</param>
		/// <param name="forceStart">Flag to determine if the task should be called immediately when the bus is started.</param>
		IEndpointConfiguration MapTask<TProducer>(string name, TimeSpan span,
		                                          bool haltOnError = true,
		                                          bool forceStart = true) where TProducer : class, IProducer;

		/// <summary>
		/// This will register all custom state machine data finders for a given assembly by name.
		/// </summary>
		/// <param name="assembly">Name of the assembly</param>
		IEndpointConfiguration ConfigureStateMachineDataMergers(string assembly);

		/// <summary>
		/// This will register all custom state machine data mergers for a given set of assemblies.
		/// </summary>
		/// <param name="assemblies">Assemblies to search</param>
		IEndpointConfiguration ConfigureStateMachineDataMergers(params Assembly[] assemblies);

		/// <summary>
		/// This will register all custom state machine data finders for a given set of assemblies.
		/// </summary>
		/// <param name="assemblies">Assemblies to search</param>
		IEndpointConfiguration ConfigureStateMachineDataFinders(params Assembly[] assemblies);

		/// <summary>
		/// This will configure an individual module to be attached to the life-cycle of the service bus.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		IEndpointConfiguration MapMessageModule<T>() where T : class, IMessageModule;

		/// <summary>
		/// This will configure all of the local bus modules 
		/// for the endpoint.
		/// </summary>
		/// <param name="assembly"></param>
		IEndpointConfiguration ConfigureBusModules(Assembly assembly);

		/// <summary>
		/// This will create the filter expression for the messages that 
		/// are associated to a remote endpoint (by name) which 
		/// will be involved in the publication/subscription model for the local service bus
		/// </summary>
		IEndpointConfiguration ReceivingMessagesFrom(Uri endpoint, Func<Type, bool> messagesFilterExpression);
	}
}