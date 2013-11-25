using System;
using System.Collections.Generic;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.MessageManagement.FaultHandling;

namespace Monaco.Bus.Repositories
{
	public interface IFaultHandlerConfigurationRepository
	{
		/// <summary>
		/// Gets the set of configured fault handlers for a message.
		/// </summary>
		IThreadSafeDictionary<Type, FaultHandlerConfiguration> Registrations { get; }

		/// <summary>
		/// This will register a fault handler configuration.
		/// </summary>
		/// <param name="configuration"></param>
		void Register(FaultHandlerConfiguration configuration);

		/// <summary>
		/// This will find a set of defined set of fault handlers for a message.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to find the fault handlers for.</typeparam>
		/// <returns></returns>
		ICollection<Type> FindHandlersForMessage<TMessage>() where TMessage : class, IMessage, new();

		/// <summary>
		/// This will find a set of defined set of fault handlers for a message.
		/// </summary>
		/// <param name="message">Type of the message to find the defined sequence of consumers for.</param>
		/// <returns></returns>
		ICollection<Type> FindHandlersForMessage(IMessage message);
	}
}