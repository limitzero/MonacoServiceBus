using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals;
using Monaco.Bus.Messages.For.Subscriptions;
using Monaco.Bus.Services.Subscriptions.Messages.Commands;
using Monaco.Bus.Services.Subscriptions.Messages.Events;
using Monaco.Extensibility.Logging;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Bus.Services.Subscriptions
{
	/// <summary>
	/// Message consumer to coordinate all actions that occur with
	///  subscriptions for this message bus instance.
	/// </summary>
	public class SubscriptionsMessageConsumer :
		Consumes<RegisterSubscriptionMessage>,
		Consumes<UnregisterSubscriptionMessage>,
		Consumes<PrepareSubscriptions>,
		Consumes<RefreshLocalSubscriptions>
	{
		private readonly IServiceBus bus;
		private readonly IControlBus controlBus;
		private readonly ISubscriptionRepository repository;

		public SubscriptionsMessageConsumer(IServiceBus bus,
											IControlBus controlBus,
											ISubscriptionRepository repository)
		{
			this.bus = bus;
			this.controlBus = controlBus;
			this.repository = repository;
		}

		public void Consume(PrepareSubscriptions message)
		{
			var msg = new SubscriptionsPrepared
						{
							At = DateTime.Now,
							Endpoint = bus.Endpoint.EndpointUri.ToString()
						};

			var subscriptionsWithComponents = repository.Subscriptions
				.Where(x => string.IsNullOrEmpty(x.Component) == false).ToList();

			if (subscriptionsWithComponents.Count() > 0)
			{
				msg.Subscriptions = subscriptionsWithComponents.Distinct().ToList();
				controlBus.Send(msg);
			}
		}

		public void Consume(RegisterSubscriptionMessage message)
		{
			repository.Register(message.Subscription);
			NoteRegistration(message.Subscription);
		}

		public void Consume(UnregisterSubscriptionMessage message)
		{
			repository.Unregister(message.Subscription);
			NoteUnRegistration(message.Subscription);

			var removed = new SubscriptionsRemoved
							{
								At = DateTime.Now,
								Endpoint = bus.Endpoint.EndpointUri.ToString(),
								Subscriptions = new List<Subscription> { message.Subscription }
							};

			controlBus.Send(removed);
		}

		public void Consume(RefreshLocalSubscriptions message)
		{
			var consumers = this.bus.FindAll<IConsumer>();
		
			foreach (var consumer in consumers)
			{
				IEnumerable<Type> theInterfaces = consumer.GetType().GetInterfaces()
					.Where(i => typeof(IConsumer).IsAssignableFrom(i))
					.Select(i => i).ToList().Distinct();

				if (theInterfaces.Count() > 0)
				{
					IEnumerable<Type> consumingMessageInterfaces =
						theInterfaces.Where(i => i.IsGenericType == true)
							.Distinct().ToList();

					IEnumerable<Type> consumedMessages = (from anInterface in consumingMessageInterfaces
														  let aMessage = anInterface.GetGenericArguments()[0]
														  where anInterface.IsGenericType == true
														  select aMessage).Distinct().ToList();

					foreach (var consumedMessage in consumedMessages)
					{
						var subscription = new Subscription
						{
							Component = consumer.GetType().FullName,
							Message = consumedMessage.FullName,
							IsActive = true,
							Uri = this.bus.Endpoint.EndpointUri.OriginalString
						};
						this.Consume(new RegisterSubscriptionMessage{Subscription = subscription});
					}
				}
			}
		}

		private void NoteRegistration(ISubscription subscription)
		{
			string message = string.Format("Registering subscription with message '{0}' on endpoint '{1}'.",
										   subscription.Message,
										   subscription.Uri);

			var logger = bus.Find<ILogger>();

			if (logger == null) return;

			logger.LogDebugMessage(message);
		}

		private void NoteUnRegistration(ISubscription subscription)
		{
			string message = string.Format("Unregistering subscription with message '{0}' on endpoint '{1}'.",
										   subscription.Message,
										   subscription.Uri);

			var logger = bus.Find<ILogger>();

			if (logger == null) return;

			logger.LogDebugMessage(message);
		}


	}
}