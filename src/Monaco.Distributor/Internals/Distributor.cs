using System;
using System.Collections.Generic;
using Castle.MicroKernel;
using Monaco.Bus.Internals.Disposable;
using Monaco.Distributor.Internals.Fabric;
using Monaco.Extensibility.Logging;

namespace Monaco.Distributor.Internals
{
	public class Distributor : BaseDisposable, IDistributor
	{
		private readonly IFabricWorkerPoolConfigurationRepository _repository;
		private readonly ILogger _logger;
		private List<IFabricWorkerPool> _currentWorkerPools;
		public bool IsRunning { get; private set; }

		public Distributor(IFabricWorkerPoolConfigurationRepository repository, 
			ILogger logger)
		{
			_repository = repository;
			_logger = logger;
			this._currentWorkerPools = new List<IFabricWorkerPool>();
		}

		~Distributor()
		{
			this.Dispose(true);
		}

		public override void CallBaseObjectDispose()
		{
			this.Stop();
		}

		public override void ReleaseManagedResources()
		{
			if(this._currentWorkerPools != null)
			{
				this._currentWorkerPools.Clear();
			}
			this._currentWorkerPools = null;
		}

		public void Start()
		{
			if(this.IsRunning) return;

			this._logger.LogInfoMessage("Distributor started.");

			foreach (var fabricWorkerPool in this._repository.FabricWorkerPoolConfigurations)
			{
				var workerPool = fabricWorkerPool.BuildWorkerPool();
				workerPool.Start();
				this._currentWorkerPools.Add(workerPool);
			}

			this.IsRunning = true;
		}

		public void Stop()
		{
			if (this._currentWorkerPools != null)
			{
				this._currentWorkerPools.ForEach(pool => pool.Stop());
			}

			this.IsRunning = false;
			this._logger.LogInfoMessage("Distributor stopped.");
		}

	}
}