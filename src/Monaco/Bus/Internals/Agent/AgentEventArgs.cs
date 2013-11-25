using System;

namespace Monaco.Bus.Internals.Agent
{
	public class AgentEventArgs : EventArgs
	{
		public AgentEventArgs()
		{
		}

		public AgentEventArgs(string message)
		{
			Message = message;
		}

		public string Message { get; private set; }
	}
}