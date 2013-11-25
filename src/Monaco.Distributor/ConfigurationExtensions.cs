using Monaco.Configuration;
using Monaco.Distributor.Internals.Fabric;
using Monaco.Distributor.Internals.Fabric.Impl;

namespace Monaco.Distributor
{
	public static class ConfigurationExtensions
	{
		/// <summary>
		/// Indicator to the runtime that this endpoint will be a distributor for messages:
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static IConfiguration UsingDistributor(this IConfiguration configuration)
		{
			configuration.Container.Register<IDistributor, Distributor.Internals.Distributor>();
			configuration.Container.Register<IFabricWorkerPool, FabricWorkerPool>();
			configuration.Container.Register<IFabricWorkerPoolConfiguration, FabricWorkerPoolConfiguration>();
			configuration.Container.Register<IFabricWorkerPoolConfigurationRepository, FabricWorkerPoolConfigurationRepository>(ContainerLifeCycle.Singleton);
			return configuration;
		}
	}
}