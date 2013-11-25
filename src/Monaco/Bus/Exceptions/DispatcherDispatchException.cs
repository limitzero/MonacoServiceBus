using System;

namespace Monaco.Bus.Exceptions
{
	/// <summary>
	/// Exception generated when the internal message dispatcher experinces an error sending the 
	/// message to the designated message handler/consumer.
	/// </summary>
	public class DispatcherDispatchException : ApplicationException
	{
		private const string _message =
			"The dispatcher failed to dispatch the message '{0}' to the component '{1}'. Reason: {2}.";

		public DispatcherDispatchException(string message, string component, Exception exception)
			: base(string.Format(_message, message, component, exception), exception)
		{
		}

		public DispatcherDispatchException(string message, string component, string reason)
			: base(string.Format(_message, message, component, reason))
		{
		}
	}
}