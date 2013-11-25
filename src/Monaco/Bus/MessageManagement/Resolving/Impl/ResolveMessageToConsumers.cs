using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals;
using Monaco.Bus.Repositories;
using Monaco.Configuration;
using Monaco.Extensions;

namespace Monaco.Bus.MessageManagement.Resolving.Impl
{
	public class ResolveMessageToConsumers : IResolveMessageToConsumers
	{
		private readonly IContainer container;

		public ResolveMessageToConsumers(IContainer container)
		{
			this.container = container;
		}

		#region IResolveMessageToConsumers Members

		public IEnumerable<IConsumer> ResolveAll(IEnvelope envelope)
		{
			var consumers = new List<IConsumer>();

			foreach (var message in envelope.Body.Payload)
			{
				consumers.AddRange(this.ResolveAll(message));
			}
			
			return consumers.Distinct().ToList();
		}

		public IEnumerable<IConsumer> ResolveAll(object message)
		{
			var consumers = new List<IConsumer>();

			// can not cast to marker interface:
			if (message == null || (message as IMessage) == null) return consumers;

			// examine for pre-configured message consumers:
			List<IConsumer> preConfiguredConsumers;
			if (TryFindPreConfiguredConsumersForMessage(message as IMessage, out preConfiguredConsumers))
			{
				consumers.AddRange(preConfiguredConsumers);
			}

			// resolve all of the consumer type implementations for the message
			// (etc. Consumes<>, StartedBy<>, OrchestratedBy<>, ...)
			List<Type> implementations = GetMessageImplementations(message.GetType());

			// pull back all of the message consumers for the list of implementations:
			foreach (Type implementation in implementations)
			{
				HashSet<IConsumer> someConsumers = container.ResolveAll(implementation).ToHashSet<IConsumer>();
				consumers.AddRange(someConsumers);
			}

			return consumers.Distinct().ToList();
		}

		#endregion

		/// <summary>
		/// This will create the generic type from the common interface-based consumer contract.
		/// methods.
		/// </summary>
		/// <param name="message">Message to create the generic interface types for.</param>
		/// <returns></returns>
		private static List<Type> GetMessageImplementations(Type message)
		{
			var consumerImplementations = new List<Type>();

			// get all of the types representing the message:
			Type type = message.GetImplementationFromProxy();
			var types = new List<Type>(type.GetInterfaces());
			types.Add(type);

			// find and remove the proxied message and use the interfaces instead:
			Type proxyMessageType = types.Find(p => p.Name.Contains("Proxy"));

			if (proxyMessageType != null)
				types.Remove(proxyMessageType);

			Type correlatedType = types.Find(c => c.Name.StartsWith(typeof (CorrelatedBy<>).Name));
			if (correlatedType != null)
				types.Remove(correlatedType);

			// add the types for the specific message types for consumer interfaces:)
			foreach (Type messageType in types)
			{
				consumerImplementations.Add(typeof (TransientConsumerOf<>).MakeGenericType(messageType));
				consumerImplementations.Add(typeof (Consumes<>).MakeGenericType(messageType));
				consumerImplementations.Add(typeof (StartedBy<>).MakeGenericType(messageType));
				consumerImplementations.Add(typeof (OrchestratedBy<>).MakeGenericType(messageType));
			}

			return consumerImplementations;
		}

		private bool TryFindPreConfiguredConsumersForMessage(IMessage message, out List<IConsumer> consumers)
		{
			bool success = false;
			consumers = new List<IConsumer>();

			var repository = container.Resolve<IHandlerConfigurationRepository>();

			if (repository == null) return success;

			ICollection<Type> configuredConsumers = repository.FindConsumersForMessage(message);

			if (configuredConsumers != null && configuredConsumers.Count > 0)
			{
				// get all of the implementations for the message by type:
				List<Type> implementations = GetMessageImplementations(message.GetType());

				// pull back all of the message consumers for the list of implementations
				// (automatically ordered because they were added to the container in an ordered fashion):
				foreach (Type implementation in implementations)
				{
					HashSet<IConsumer> someConsumers = container.ResolveAll(implementation).ToHashSet<IConsumer>();
					consumers.AddRange(someConsumers);
				}
			}

			success = consumers.Count > 0;

			return success;
		}
	}
}