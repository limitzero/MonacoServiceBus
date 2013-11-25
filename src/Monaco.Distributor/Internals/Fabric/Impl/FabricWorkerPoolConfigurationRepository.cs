using System;
using System.Collections.Generic;

namespace Monaco.Distributor.Internals.Fabric.Impl
{
	public class FabricWorkerPoolConfigurationRepository : IFabricWorkerPoolConfigurationRepository
	{
		public HashSet<IFabricWorkerPoolConfiguration> FabricWorkerPoolConfigurations { get; private set; }

		public FabricWorkerPoolConfigurationRepository()
		{
			this.FabricWorkerPoolConfigurations = new HashSet<IFabricWorkerPoolConfiguration>();
		}

		public void Add(IFabricWorkerPoolConfiguration fabricWorkerPoolConfiguration)
		{
			this.FabricWorkerPoolConfigurations.Add(fabricWorkerPoolConfiguration);
		}

		public void Remove(IFabricWorkerPoolConfiguration fabricWorkerPoolConfiguration)
		{
			this.FabricWorkerPoolConfigurations.Remove(fabricWorkerPoolConfiguration);
		}

		public void Clear()
		{
			this.FabricWorkerPoolConfigurations.Clear();
		}
	}
}