using System;
using log4net;
using Monaco.Bus.Internals;

namespace Monaco.Hosting
{
	public class HostedService : IStartable
	{
		private readonly AppDomain _appDomain;
		private readonly string _assemblyName;

		private readonly ILog log = LogManager.GetLogger(typeof (HostedService));

		public HostedService(IApplicationHost host, string assemblyName, AppDomain appDomain)
		{
			Host = host;

			if (!string.IsNullOrEmpty(_assemblyName))
				_assemblyName = assemblyName.Replace(".dll", string.Empty).Replace(".exe", string.Empty);
			;

			_appDomain = appDomain;
		}

		public IApplicationHost Host { get; private set; }

		#region IStartable Members

		public bool IsRunning { get; private set; }

		public void Dispose()
		{
			Stop();
		}

		public void Start()
		{
			IsRunning = true;
			Host.Start(_assemblyName);
		}

		public void Stop()
		{
			Host.Stop();

			try
			{
				AppDomain.Unload(_appDomain);
			}
			catch (AppDomainUnloadedException appDomainUnloadedException)
			{
				log.Error("Could not unload the app domain, most likely there is a thread that could not be aborted. Reason: " +
				          appDomainUnloadedException);
			}
			finally
			{
				Host = null;
			}

			IsRunning = false;
		}

		#endregion
	}
}