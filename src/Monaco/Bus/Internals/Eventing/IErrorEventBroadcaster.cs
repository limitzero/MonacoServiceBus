using System;

namespace Monaco.Bus.Internals.Eventing
{
	/// <summary>
	/// Contract for all components that wish to broadcast error messages
	/// to the infrastructure for processing.
	/// </summary>
	public interface IErrorEventBroadcaster
	{
		event EventHandler<ComponentErrorEventArgs> ComponentErrorEvent;
	}
}