using System;

namespace Monaco.Bus.Internals.Eventing
{
	public class ComponentStartedEventArgs : EventArgs
	{
		public ComponentStartedEventArgs(string componentName)
		{
			ComponentName = componentName;
		}

		public string ComponentName { get; set; }
	}
}