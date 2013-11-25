using System;

namespace Monaco.Bus.Internals.Eventing
{
	public class ComponentErrorEventArgs : EventArgs
	{
		public ComponentErrorEventArgs(string errorMessage)
			: this(errorMessage, null)
		{
		}

		public ComponentErrorEventArgs(Exception exception)
			: this(string.Empty, exception)
		{
		}

		public ComponentErrorEventArgs(string errorMessage, Exception exception)
		{
			ErrorMessage = errorMessage;
			Exception = exception;
		}

		public string ErrorMessage { get; set; }
		public Exception Exception { get; set; }
	}
}