using System;

namespace Monaco.Extensibility.Logging
{
	public interface ILogger : IDisposable
	{
		void LogDebugMessage(string message);
		void LogInfoMessage(string message);

		void LogWarnMessage(string message);
		void LogWarnMessage(string message, Exception exception);

		void LogErrorMessage(string message);
		void LogErrorMessage(string message, Exception exception);
	}
}