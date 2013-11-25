using System.Collections.Generic;

namespace Monaco.Distributor.Internals.Fabric
{
	public interface IFabricWorkerPoolConfigurationRepository
	{
		HashSet<IFabricWorkerPoolConfiguration> FabricWorkerPoolConfigurations{ get; }
		void Add(IFabricWorkerPoolConfiguration fabricWorkerPoolConfiguration);
		void Remove(IFabricWorkerPoolConfiguration fabricWorkerPoolConfiguration);
		void Clear();
	}
}