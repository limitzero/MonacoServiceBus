using System;
using System.Collections.Generic;
using Monaco.Bus.Internals.Collections;
using Monaco.Configuration;
using Monaco.Configuration.Registration;

namespace Monaco.Bus.Repositories
{
	/// <summary>
	/// Repository for holding resolved message to consumer implementations.
	/// </summary>
	public interface IHandlerConfigurationRepository
	{
		/// <summary>
		/// Gets the set of configured message to consumer implementations.
		/// </summary>
		IThreadSafeDictionary<Type, HandlerConfiguration> Registrations { get; }

		/// <summary>
		/// This will register a handler configuration.
		/// </summary>
		/// <param name="configuration"></param>
		void Register(HandlerConfiguration configuration);

		/// <summary>
		/// This will find a set of defined message consumers for a message.
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to find the defined sequence of consumers for.</typeparam>
		/// <returns></returns>
		ICollection<Type> FindConsumersForMessage<TMessage>() where TMessage : class, IMessage, new();

		/// <summary>
		/// This will find a set of defined message consumers for a message.
		/// </summary>
		/// <param name="message">Type of the message to find the defined sequence of consumers for.</param>
		/// <returns></returns>
		ICollection<Type> FindConsumersForMessage(object message);
	}
}