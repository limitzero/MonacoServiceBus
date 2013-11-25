using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monaco.Bus.Exceptions;
using Monaco.Bus.Internals;
using Monaco.Extensibility.Logging;
using Monaco.Extensions;

namespace Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers.Impl
{
	public class SimpleConsumerMessageDispatcher : ISimpleConsumerMessageDispatcher
	{
		private readonly ILogger logger;
		private IServiceBus bus;

		public SimpleConsumerMessageDispatcher(ILogger logger)
		{
			this.logger = logger;
		}

		#region ISimpleConsumerMessageDispatcher Members

		public void Dispatch(IServiceBus bus, IConsumer handler, IEnvelope envelope)
		{
			object theComponent = handler;
			this.bus = bus;

			logger.LogDebugMessage(
					string.Format("Start: dispatching message '{0}' to component '{1}'.",
								 envelope.Body.Payload.ToItemList(),
								  handler.GetType().FullName));

			envelope.Header.RecordStage(handler, envelope.Body.Payload.ToItemList(), "Dispatch");

			if (theComponent != null)
			{
				foreach (var message in envelope.Body.Payload)
				{
					try
					{
						DispatchMessage(theComponent, message);
					}
					catch (Exception e)
					{
						var ex = new DispatcherDispatchException(message.GetImplementationFromProxy().FullName,
																 handler.GetType().FullName, e);
						throw ex;
					}
					finally
					{
						logger.LogDebugMessage(
							string.Format("Complete: dispatching message '{0}' to component '{1}'.",
										  message.GetImplementationFromProxy().FullName,
										  handler.GetType().FullName));
					}
				}
			}
		}

		#endregion

		private void DispatchMessage(object component, object message)
		{
			SetServiceBus(component, false);

			if (typeof(MessageConsumer).IsAssignableFrom(component.GetType()))
			{
				DispatchMessageToDslMessageConsumer(component, message);
				return;
			}

			ConsumeMessage(component, message);

			SetServiceBus(component, true);
		}

		private void DispatchMessageToDslMessageConsumer(object component, object message)
		{
			var consumer = component as MessageConsumer;

			consumer.Bus = bus;
			consumer.CurrentMessage = message;
			consumer.Define();

			List<Action> conditions = null;
			consumer.Conditions.TryGetValue(message.GetType(), out conditions);

			if (conditions != null)
			{
				conditions.ForEach(x => x.Invoke());
			}
			else
			{
				// no conditions on the DSL consumer:
				ConsumeMessage(component, message);
			}
		}

		private void SetServiceBus(object component, bool canRemove)
		{
			// setter injected bus instance:
			PropertyInfo serviceBusProperty = (from property in component.GetType().GetProperties()
											   where property.PropertyType == typeof(IServiceBus)
											   select property).FirstOrDefault();

			if (serviceBusProperty != null)
			{
				if (canRemove == false)
				{
					((MessageConsumer)component).Bus = this.bus;
				}
				else
				{
					((MessageConsumer)component).Bus = null;
				}
			}
		}

		private void ConsumeMessage(object component, object message)
		{
			MethodInfo consumerMethod =
				new MessageToMethodMapper().Map(component, message);

			if (consumerMethod != null)
			{
				new MessageMethodInvoker().Invoke(component, consumerMethod, message);
			}
		}
	}
}