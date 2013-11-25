using System.Collections.Generic;
using Monaco.Bus.Internals;

namespace Monaco.Bus.MessageManagement.Dispatcher
{
	/// <summary>
	/// Contract to dispatch messages to consumers based on consumer type.
	/// </summary>
	public interface IMessageDispatcher
	{
		void Dispatch(IServiceBus bus, IEnvelope envelope);
		void Dispatch(IServiceBus bus, IEnumerable<IConsumer> consumers, IEnvelope envelope);
	}
}