using System;

namespace Monaco.Extensibility.Logging.Impl
{
	public class NullLogger : ILogger
	{
		#region ILogger Members

		public void Dispose()
		{
		}

		public void LogDebugMessage(string message)
		{
		}

		public void LogInfoMessage(string message)
		{
		}

		public void LogWarnMessage(string message)
		{
		}

		public void LogWarnMessage(string message, Exception exception)
		{
		}

		public void LogErrorMessage(string message)
		{
		}

		public void LogErrorMessage(string message, Exception exception)
		{
		}

		#endregion
	}
}