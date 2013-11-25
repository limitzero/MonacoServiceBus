using System;

namespace Monaco.Bus.Internals.Agent
{
	public class AgentErrorEventArgs : EventArgs
	{
		public AgentErrorEventArgs()
		{
		}

		public AgentErrorEventArgs(string name, string message, Exception exception)
		{
			Name = name;
			Message = message;
			Exception = exception;
		}

		public string Name { get; set; }
		public string Message { get; private set; }
		public Exception Exception { get; private set; }
	}
}