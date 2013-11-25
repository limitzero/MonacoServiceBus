using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Internals.Agent
{
	/// <summary>
	/// Contract for agent manager that will collectively manage the lifecycle of the 
	/// background agents for the message bus instance.
	/// </summary>
	public interface IAgentManager : IStartable, IStartableEventBroadcaster, IErrorEventBroadcaster
	{
	}
}