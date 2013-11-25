using System;
using System.Collections.Generic;
using Monaco.Distributor.Internals.Fabric.Impl;

namespace Monaco.Distributor.Internals.Fabric
{
	public interface IFabricWorkerPoolConfiguration
	{
		Uri NodeEndpoint { get; set; }
		int NumberOfThreads { get; set; }
		int NumberOfRequestsForPoolWorker { get; set; }
		ICollection<IFabricWorkerConfiguration> FabricWorkerConfigurations { get; }
		void Add(FabricWorkerConfiguration fabricWorkerConfiguration);
		IFabricWorkerPool BuildWorkerPool();
	}
}