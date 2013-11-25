using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Monaco.Distributor.Internals.Fabric.Impl;

namespace Monaco.Distributor.Internals.Fabric.Policies
{
	public class FindNextWorkerBySmallestWeightPolicy : IWorkerSelectionPolicy
	{
		public int SortOrder
		{
			get { return 1; }
		}

		public FabricWorker Select(ICollection<FabricWorker> fabricWorkers)
		{
			var nextWorker = (from worker in fabricWorkers
			                  orderby worker.Weight ascending
			                  select worker).FirstOrDefault();
			return nextWorker;
		}
	}
}