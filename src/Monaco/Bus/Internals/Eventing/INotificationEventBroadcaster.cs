using System;

namespace Monaco.Bus.Internals.Eventing
{
	/// <summary>
	/// Contract for components that will send informational events 
	/// throughout the infrastructure.
	/// </summary>
	public interface INotificationEventBroadcaster
	{
		/// <summary>
		/// This is the event that can be triggered for informational messages.
		/// </summary>
		event EventHandler<ComponentNotificationEventArgs> ComponentNotificationEvent;
	}
}