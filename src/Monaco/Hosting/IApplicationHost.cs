using System;

namespace Monaco.Hosting
{
	public interface IApplicationHost : IDisposable
	{
		void Start(string assemblyName);
		void Stop();
		void SetConfigurationFile(string configurationFile);
		void SetHostAssembly(string assemblyName);
		void SetEndpoint(string endpointConfiguration);
	}
}