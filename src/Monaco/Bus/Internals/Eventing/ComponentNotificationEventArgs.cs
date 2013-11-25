using System;

namespace Monaco.Bus.Internals.Eventing
{
	public enum NotificationLevel
	{
		Debug,
		Info,
		Warn
	}

	public class ComponentNotificationEventArgs : EventArgs
	{
		public ComponentNotificationEventArgs(NotificationLevel level, string message)
		{
			Level = level;
			Message = message;
		}

		public ComponentNotificationEventArgs(string message)
			: this(NotificationLevel.Debug, message)
		{
			Message = message;
		}

		public NotificationLevel Level { get; private set; }
		public string Message { get; private set; }
	}
}