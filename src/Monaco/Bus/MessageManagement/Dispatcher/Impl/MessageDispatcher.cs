using System.Collections.Generic;
using Monaco.Bus.Internals;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.Consumers;
using Monaco.Bus.MessageManagement.Dispatcher.Internal.StateMachines;
using Monaco.Bus.MessageManagement.Resolving;
using Monaco.Configuration;
using Monaco.StateMachine;

namespace Monaco.Bus.MessageManagement.Dispatcher.Impl
{
	public class MessageDispatcher : IMessageDispatcher
	{
		private readonly IContainer container;

		public MessageDispatcher(IContainer container)
		{
			this.container = container;
		}

		#region IMessageDispatcher Members

		public void Dispatch(IServiceBus bus, IEnvelope envelope)
		{
			var resolver = container.Resolve<IResolveMessageToConsumers>();
			var consumers = resolver.ResolveAll(envelope);
			this.Dispatch(bus, consumers, envelope);
		}

		public void Dispatch(IServiceBus bus, IEnumerable<IConsumer> consumers, IEnvelope envelope)
		{
			foreach (IConsumer consumer in consumers)
			{
				if (typeof (SagaStateMachine).IsAssignableFrom(consumer.GetType()))
				{
					var dispatcher = container.Resolve<ISagaStateMachineMessageDispatcher>();
					dispatcher.Dispatch(bus, consumer, envelope);
				}
				else
				{
					var dispatcher = container.Resolve<ISimpleConsumerMessageDispatcher>();
					dispatcher.Dispatch(bus, consumer, envelope);
				}
			}
		}

		#endregion
	}
}