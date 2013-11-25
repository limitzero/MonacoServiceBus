using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Monaco.Bus;
using Monaco.Bus.Internals.Agent;
using Monaco.Endpoint.Factory;
using Monaco.Extensibility.Logging;
using Monaco.Transport;

namespace Monaco.Distributor.Internals.Fabric.Impl
{
	public class FabricWorkerPool : IFabricWorkerPool
	{
		private Exchange _fabricWorkerPoolExchange;
		private readonly ILogger _logger;
		private readonly IEndpointFactory _endpointFactory;
		private IFabricWorkerPoolConfiguration _configuration;
		private List<FabricWorker> _fabricWorkers;
		private bool _disposed;
		private int _currentReceivedRequests;
		private Random optimizer;
		public Uri LoadBalancedEndpoint { get; set; }
		public bool IsRunning { get; private set; }

		public FabricWorkerPool(
			ILogger logger,
			IEndpointFactory endpointFactory)
		{
			_logger = logger;
			_endpointFactory = endpointFactory;
			this._fabricWorkers = new List<FabricWorker>();
		}

		public void Dispose()
		{
			this.Stop();
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Start()
		{
			if (this.IsRunning) return;

			this._fabricWorkerPoolExchange = this._endpointFactory.Build(this.LoadBalancedEndpoint);
			this._fabricWorkerPoolExchange.Transport.OnMessageReceived += OnFabricWorkerPoolMessageReceived;
			this._fabricWorkerPoolExchange.Transport.NumberOfWorkerThreads = this._configuration.NumberOfThreads;
			((BaseAgent)this._fabricWorkerPoolExchange.Transport).Start();

			this._logger.LogInfoMessage(string.Format("Worker pool on load-balanced endpoint '{0}' started.", this.LoadBalancedEndpoint.OriginalString));

			this.IsRunning = true;
		}

		public void Stop()
		{
			this._fabricWorkerPoolExchange.Transport.OnMessageReceived -= OnFabricWorkerPoolMessageReceived;
			((BaseAgent)this._fabricWorkerPoolExchange.Transport).Stop();

			this._logger.LogInfoMessage(string.Format("Worker pool on load-balanced endpoint '{0}' stopped.", this.LoadBalancedEndpoint.OriginalString));
			this.IsRunning = false;
		}

		public void Recycle()
		{
			this._currentReceivedRequests = 0;
			this._fabricWorkers.ForEach(worker => worker.Recycle());
		}

		public void Configure(IFabricWorkerPoolConfiguration configuration)
		{
			this._configuration = configuration;
			this.LoadBalancedEndpoint = this._configuration.NodeEndpoint;
			this.BuildFabricWorkers();
		}

		private void BuildFabricWorkers()
		{
			if (this._configuration != null)
			{
				this.optimizer = new Random(this._configuration.FabricWorkerConfigurations.Count);

				for (int index = 0; index < this._configuration.FabricWorkerConfigurations.Count; index++)
				{
					var workerConfiguration =
						new List<IFabricWorkerConfiguration>(this._configuration.FabricWorkerConfigurations)[index];

					var fabricWorker = new FabricWorker
										{
											LoadBalancedEndpoint = this._configuration.NodeEndpoint,
											Endpoint = workerConfiguration.FabricWorkerEndpoint,
											Weight = workerConfiguration.Weight.HasValue ? workerConfiguration.Weight.Value : 0,
											Requests = workerConfiguration.Requests.HasValue ? workerConfiguration.Requests.Value : 0
										};

					fabricWorker.Initialize(this._configuration.FabricWorkerConfigurations.Count, optimizer);

					this._fabricWorkers.Add(fabricWorker);
				}
			}
		}

		private void OnFabricWorkerPoolMessageReceived(object sender, MessageReceivedEventArgs e)
		{
			this._currentReceivedRequests++;
			this.Dispatch(e.Envelope);
		}

		private void Dispatch(IEnvelope envelope)
		{
			var policychain = new FabricWorkerSelector(this._logger);
			var nextWorker = policychain.Select(this._fabricWorkers, this.LoadBalancedEndpoint, this._currentReceivedRequests);

			if (nextWorker != null)
			{
				var exchange = this._endpointFactory.Build(nextWorker.Endpoint);

				if (exchange != null)
				{
					exchange.Transport.Send(envelope);
				}

				this._logger.LogInfoMessage(string.Format("Worker for endpoint '{0}' selected for dispatch of message '{1}'.",
					nextWorker.Endpoint.OriginalString, 
					envelope.Body.Label));
			}
		}

		private void Dispose(bool disposing)
		{
			if (disposing == true)
			{
				if (this._fabricWorkers != null)
				{
					this._fabricWorkers.Clear();
				}
				this._fabricWorkers = null;

				((BaseAgent)this._fabricWorkerPoolExchange.Transport).Dispose();
				this._fabricWorkerPoolExchange = null;
			}
			this._disposed = true;
		}
	}
}