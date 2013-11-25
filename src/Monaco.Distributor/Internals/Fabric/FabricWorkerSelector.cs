using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Distributor.Internals.Fabric.Impl;
using Monaco.Extensibility.Logging;

namespace Monaco.Distributor.Internals.Fabric
{
	public class FabricWorkerSelector
	{
		private readonly ILogger _logger;

		public FabricWorkerSelector(ILogger logger)
		{
			_logger = logger;
		}

		public FabricWorker Select(ICollection<FabricWorker> workers, Uri endpoint, int numberOfMessagesReceived)
		{
			FabricWorker nextWorker = null;

			while (nextWorker == null)
			{
				var availableWorkers = (from match in workers
				              let requestCount = match.GetNumberOfRequestsForWeighting(numberOfMessagesReceived)
				              where  match.SelectionTotal < requestCount
							  orderby requestCount ascending 
				              select match).ToList();

				if(availableWorkers.Count > 0)
				{
					// find the worker that has the least amount of selections for handling a message:
					nextWorker = availableWorkers.OrderBy(w => w.SelectionTotal).First();

					// increase the "session" characteristics (i.e. add one more to the count of handled messages):
					nextWorker.AppendToSession();
				}
				else
				{
					// must recycle the set of workers in order to find one that can handle the request:
					new List<FabricWorker>(workers).ForEach(w => w.Recycle());
					this._logger.LogDebugMessage("No worker selected, recycling workers for next selection round.");
				}

			}

			this._logger.LogDebugMessage(nextWorker.ToString());
			return nextWorker;
		}
	}
}