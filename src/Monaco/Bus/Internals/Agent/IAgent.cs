using Monaco.Bus.Internals.Eventing;

namespace Monaco.Bus.Internals.Agent
{
	/// <summary>
	/// Contract for any service that will run in the background.
	/// </summary>
	public interface IAgent : IStartable, IPollable, IPausable, IStartableEventBroadcaster
	{
	}
}