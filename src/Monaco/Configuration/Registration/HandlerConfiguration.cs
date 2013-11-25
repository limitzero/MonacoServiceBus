using System;
using System.Collections.Generic;
using Monaco.Bus.Internals;

namespace Monaco.Configuration.Registration
{
	/// <summary>
	/// Class to hold the configuration for a message that has to be processed by a defined set of message consumers.
	/// </summary>
	public class HandlerConfiguration
	{
		public HandlerConfiguration()
		{
			Consumers = new HashSet<Type>();
		}

		/// <summary>
		/// Gets the mesasge that has a specific set of sequential handlers for processing.
		/// </summary>
		public Type Message { get; private set; }

		/// <summary>
		/// Gets the set of consumers to handle the message.
		/// </summary>
		public HashSet<Type> Consumers { get; private set; }

		/// <summary>
		/// This will set the message that the specific set of sequential consumers should be configured to. 
		/// </summary>
		/// <typeparam name="TMessage">Type of the message to set the specific handling chain for.</typeparam>
		/// <returns></returns>
		public HandlerConfiguration ForMessage<TMessage>() where TMessage : class, IMessage
		{
			Message = typeof (TMessage);
			return this;
		}

		/// <summary>
		/// This will configure the first message consumer to handle the message.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer to handle the message.</typeparam>
		/// <returns></returns>
		public HandlerConfiguration InitiallyHandledBy<TConsumer>() where TConsumer : IConsumer
		{
			Type consumerType = typeof (Consumes<>).MakeGenericType(Message);

			if (consumerType.IsAssignableFrom(typeof (TConsumer)))
				Consumers.Add(typeof (TConsumer));

			return this;
		}

		/// <summary>
		/// This will configure the additional message consumer to handle the message.
		/// </summary>
		/// <typeparam name="TConsumer">Type of the consumer to handle the message.</typeparam>
		/// <returns></returns>
		public HandlerConfiguration FollowedBy<TConsumer>() where TConsumer : IConsumer
		{
			Type consumerType = typeof (Consumes<>).MakeGenericType(Message);

			if (consumerType.IsAssignableFrom(typeof (TConsumer)))
				Consumers.Add(typeof (TConsumer));

			return this;
		}
	}
}