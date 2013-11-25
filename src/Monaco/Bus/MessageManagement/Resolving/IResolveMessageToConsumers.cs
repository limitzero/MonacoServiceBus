using System.Collections.Generic;
using Monaco.Bus.Internals;

namespace Monaco.Bus.MessageManagement.Resolving
{
	public interface IResolveMessageToConsumers
	{
		IEnumerable<IConsumer> ResolveAll(IEnvelope envelope);
		IEnumerable<IConsumer> ResolveAll(object message);
	}
}