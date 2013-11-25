using System;
using Monaco.Bus.Internals;

namespace Monaco.Distributor.Internals.Fabric
{
	public interface IFabricWorkerPool : IStartable, IRecyclable
	{
		Uri LoadBalancedEndpoint { get; set; }
		void Configure(IFabricWorkerPoolConfiguration configuration);
	}
}