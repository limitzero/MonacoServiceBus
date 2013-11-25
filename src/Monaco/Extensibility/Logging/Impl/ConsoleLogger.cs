using System;
using System.Diagnostics;

namespace Monaco.Extensibility.Logging.Impl
{
	public class ConsoleLogger : ILogger
	{
		#region ILogger Members

		public void Dispose()
		{
		}

		public void LogDebugMessage(string message)
		{
			Debug.WriteLine("DEBUG: " + message);
			Console.WriteLine("DEBUG: " + message);
		}

		public void LogInfoMessage(string message)
		{
			Debug.WriteLine("INFO: " + message);
			Console.WriteLine("INFO: " + message);
		}

		public void LogWarnMessage(string message)
		{
			Debug.WriteLine("WARN: " + message);
			Console.WriteLine("WARN: " + message);
		}

		public void LogWarnMessage(string message, Exception exception)
		{
			Debug.WriteLine("WARN: " + message + " EXCEPTION: " + exception);
			Console.WriteLine("WARN: " + message + " EXCEPTION: " + exception);
		}

		public void LogErrorMessage(string message)
		{
			Debug.WriteLine("ERROR: " + message);
			Console.WriteLine("ERROR: " + message);
		}

		public void LogErrorMessage(string message, Exception exception)
		{
			Debug.WriteLine("ERROR: " + message + " EXCEPTION: " + exception);
			Console.WriteLine("ERROR: " + message + " EXCEPTION: " + exception);
		}

		#endregion
	}
}