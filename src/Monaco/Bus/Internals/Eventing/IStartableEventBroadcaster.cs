using System;

namespace Monaco.Bus.Internals.Eventing
{
	/// <summary>
	/// Contract for all components that can be started and broadcasting the 
	/// events through the infrastructure.
	/// </summary>
	public interface IStartableEventBroadcaster
	{
		event EventHandler<ComponentStartedEventArgs> ComponentStartedEvent;
		event EventHandler<ComponentStoppedEventArgs> ComponentStoppedEvent;
	}
}