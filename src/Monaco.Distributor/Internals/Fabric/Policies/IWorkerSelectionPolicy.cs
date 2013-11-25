using System.Collections.Generic;
using Monaco.Distributor.Internals.Fabric.Impl;

namespace Monaco.Distributor.Internals.Fabric.Policies
{
	public interface IWorkerSelectionPolicy
	{
		int SortOrder { get;  }
		FabricWorker Select(ICollection<FabricWorker> fabricWorkers);
	}
}