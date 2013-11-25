using System;

namespace Monaco.Bus.Internals.Eventing
{
	public class ComponentStoppedEventArgs : EventArgs
	{
		public ComponentStoppedEventArgs(string componentName)
		{
			ComponentName = componentName;
		}

		public string ComponentName { get; set; }
	}
}