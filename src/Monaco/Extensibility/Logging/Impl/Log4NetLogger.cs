using System;
using System.IO;
using log4net;
using log4net.Config;
using Monaco.Bus;

namespace Monaco.Extensibility.Logging.Impl
{
	public class Log4NetLogger : ILogger
	{
		private readonly bool _isConfigured;
		private bool _disposed;
		private ILog _logger;

		public Log4NetLogger()
		{
			if (_isConfigured == false)
			{
				try
				{
					XmlConfigurator.ConfigureAndWatch(new FileInfo(@"log4net.config.xml"));
				}
				catch (Exception exception)
				{
					string msg = "An error has occurred while configuring log4net. Reason:" + exception.Message +
					             " Please ensure that the configuration file named 'log4net.config.xml' is placed in the executable directory.";
					throw new Exception(msg, exception);
				}

				_isConfigured = true;
				GetLogger();
			}
		}

		#region ILogger Members

		public void LogDebugMessage(string message)
		{
			if (_disposed) return;

			if (_logger.IsDebugEnabled)
				_logger.Debug(message);

			SendToLogEndpoint("DEBUG", message);
		}

		public void LogInfoMessage(string message)
		{
			if (_disposed) return;

			if (_logger.IsInfoEnabled)
				_logger.Info(message);

			SendToLogEndpoint("INFO", message);
		}

		public void LogWarnMessage(string message)
		{
			if (_disposed) return;

			if (_logger.IsWarnEnabled)
				_logger.Warn(message);

			SendToLogEndpoint("WARN", message);
		}

		public void LogWarnMessage(string message, Exception exception)
		{
			if (_disposed) return;

			if (_logger.IsWarnEnabled)
				_logger.Warn(message, exception);

			SendToLogEndpoint("WARN", message, exception);
		}

		public void LogErrorMessage(string message)
		{
			if (_disposed) return;

			if (_logger.IsErrorEnabled)
				_logger.Error(message);

			SendToLogEndpoint("ERROR", message);
		}

		public void LogErrorMessage(string message, Exception exception)
		{
			if (_disposed) return;

			if (_logger.IsErrorEnabled)
				if (string.IsNullOrEmpty(message))
				{
					message = exception.Message;
				}
			_logger.Error(message, exception);

			SendToLogEndpoint("ERROR", message, exception);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#endregion

		public virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_logger = null;
				}

				_disposed = true;
			}
		}

		private void GetLogger()
		{
			if (_logger == null)
				_logger = LogManager.GetLogger(typeof (DefaultServiceBus));
		}

		private void SendToLogEndpoint(string level, string message, Exception exception = null)
		{
			//    var endpoint = this._bus.Find<ILogEndpoint>();

			//    if(endpoint == null) return;

			//    var endpointLogMessage = new EndpointLogMessage
			//                                {
			//                                    Endpoint = this._bus.Transport.EndpointUri,
			//                                    Exception = exception == null ? string.Empty : exception.ToString(),
			//                                    Level = level,
			//                                    Message = message
			//                                };

			//    endpoint.Receive(endpointLogMessage);
		}
	}
}