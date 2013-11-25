using System;
using Castle.Core.Configuration;
using Monaco.Configuration;
using Monaco.Configuration.Elements;
using Monaco.Distributor.Internals.Fabric;
using Monaco.Distributor.Internals.Fabric.Impl;
using Monaco.Endpoint.Factory;
using Monaco.Transport;

namespace Monaco.Distributor.Configuration.Elements
{
	public class FabricWorkerPoolElementBuilder : BaseElementBuilder
	{
		private const string elementName = "worker.pools";

		public override bool IsMatchFor(string name)
		{
			return name.Trim().ToLower().Equals(elementName.Trim().ToLower());
		}

		public override void Build(Castle.Core.Configuration.IConfiguration configuration)
		{
			string distributorEndpoint = string.Empty;

			for (int pool = 0; pool < configuration.Children.Count; pool++)
			{
				var workerPool = configuration.Children[pool];

				string endpoint = workerPool.Attributes["endpoint"];
				if(string.IsNullOrEmpty(distributorEndpoint))
				{
					distributorEndpoint = endpoint;
				}

				int threads = 0;
				Int32.TryParse(workerPool.Attributes["threads"], out threads);

				int maxrequests = 0;
				Int32.TryParse(workerPool.Attributes["requests"], out maxrequests);

				var workerPoolConfiguration = Container.Resolve<IFabricWorkerPoolConfiguration>();
				workerPoolConfiguration.NodeEndpoint = new Uri(endpoint);
				workerPoolConfiguration.NumberOfRequestsForPoolWorker = maxrequests == 0 ? 100 : maxrequests;
				workerPoolConfiguration.NumberOfThreads = threads > 0 ? threads : 1;

				for (int worker = 0; worker < workerPool.Children.Count; worker++)
				{
					var configuredWorker = workerPool.Children[worker];
					BuildWorkersForPool(workerPoolConfiguration, configuredWorker);
				}

				var repository = Container.Resolve<IFabricWorkerPoolConfigurationRepository>();
				repository.Add(workerPoolConfiguration);
			}

			if(string.IsNullOrEmpty(distributorEndpoint) == false)
			{
				// register the transport and endpoint semantics with the distributor:
				Exchange exchange = Container.Resolve<IEndpointFactory>().Build(new Uri(distributorEndpoint));
				if (exchange != null)
				{
					Container.RegisterInstance<ITransport>(exchange.Transport);
				}
			}
		}

		private static void BuildWorkersForPool(IFabricWorkerPoolConfiguration workerPoolConfiguration,
			Castle.Core.Configuration.IConfiguration configuredWorker)
		{
				string endpoint = configuredWorker.Attributes["endpoint"];

				double weight = 0;
				double.TryParse(configuredWorker.Attributes["weight"], out weight);

				int requests = 0;
				Int32.TryParse(configuredWorker.Attributes["requests"], out requests);
				if(requests == 0)
				{
					requests = workerPoolConfiguration.NumberOfRequestsForPoolWorker;
				}

			var fabricWorkerConfiguration = new FabricWorkerConfiguration
				                                	{
				                                		FabricWorkerEndpoint = new Uri(endpoint),
				                                		Requests = requests,
				                                		Weight = weight
				                                	};

				workerPoolConfiguration.Add(fabricWorkerConfiguration);
		}
	}
}