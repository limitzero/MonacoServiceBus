using System;
using System.Collections.Generic;
using Castle.MicroKernel;

namespace Monaco.Distributor.Internals.Fabric.Impl
{
	public class FabricWorkerPoolConfiguration : IFabricWorkerPoolConfiguration
	{
		private readonly IKernel _kernel;
		public Uri NodeEndpoint { get; set; }
		public int NumberOfThreads { get; set; }
		public int NumberOfRequestsForPoolWorker { get; set; }
		public ICollection<IFabricWorkerConfiguration> FabricWorkerConfigurations { get; private set; }

		public FabricWorkerPoolConfiguration(IKernel kernel)
		{
			_kernel = kernel;
			this.FabricWorkerConfigurations = new List<IFabricWorkerConfiguration>();
		}

		public void Add(FabricWorkerConfiguration fabricWorkerConfiguration)
		{
			this.FabricWorkerConfigurations.Add(fabricWorkerConfiguration);
		}

		public IFabricWorkerPool BuildWorkerPool()
		{
			var pool = this._kernel.Resolve<IFabricWorkerPool>();
			pool.Configure(this);
			return pool;
		}
	}
}