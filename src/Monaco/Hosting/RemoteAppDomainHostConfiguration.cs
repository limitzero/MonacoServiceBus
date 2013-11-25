using System.IO;
using System.Reflection;
using Monaco.Configuration;
using Monaco.Configuration.Endpoint;

namespace Monaco.Hosting
{
	public class RemoteAppDomainHostConfiguration
	{
		public string EndpointConfig { get; private set; }
		public string ConfigurationFile { get; set; }
		public string HostAssembly { get; private set; }
		public string HostAssemblyLocation { get; private set; }

		/// <summary>
		/// The directory containing the host assembly where the app domain will be created .
		/// </summary>
		/// <param name="location">Directory where the host assembly is located</param>
		/// <returns></returns>
		public RemoteAppDomainHostConfiguration HostAssemblyDirectoryLocationOf(string location)
		{
			HostAssemblyLocation = location;
			return this;
		}

		/// <summary>
		/// The name of the library containing the components to host in the service bus.
		/// </summary>
		/// <param name="assemblyName"></param>
		/// <returns></returns>
		public RemoteAppDomainHostConfiguration HostAssemblyNameOf(string assemblyName)
		{
			HostAssembly = assemblyName;
			return this;
		}

		/// <summary>
		/// The assembly of the library containing the components to host in the service bus.
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public RemoteAppDomainHostConfiguration HostAssemblyNameOf(Assembly assembly)
		{
			HostAssembly = Path.GetFileName(assembly.Location);
			HostAssemblyLocation = Path.GetDirectoryName(assembly.Location);
			return this;
		}

		/// <summary>
		/// This will set the the configuration file for the service bus that is hosted 
		/// in the app domain. 
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public RemoteAppDomainHostConfiguration ConfigurationFileNameOf(string file)
		{
			ConfigurationFile = file;
			return this;
		}

		/// <summary>
		/// This will set the specific endpoint configuration that the bus will use to configure all consumers, etc.
		/// </summary>
		/// <typeparam name="TConfiguration"></typeparam>
		/// <returns></returns>
		public RemoteAppDomainHostConfiguration EndpointConfigurationOf<TConfiguration>()
			where TConfiguration : class, ICanConfigureEndpoint
		{
			EndpointConfig = typeof (TConfiguration).FullName;
			HostAssemblyNameOf(typeof (TConfiguration).Assembly);
			return this;
		}

		/// <summary>
		/// This will set the specific endpoint configuration type name  that the bus will use to configure all consumers, etc.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <returns></returns>
		public RemoteAppDomainHostConfiguration EndpointConfigurationOf(string endpoint)
		{
			EndpointConfig = endpoint;
			return this;
		}
	}
}