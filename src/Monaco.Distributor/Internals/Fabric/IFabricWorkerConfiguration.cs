using System;

namespace Monaco.Distributor.Internals.Fabric
{
	public interface IFabricWorkerConfiguration
	{
		Uri FabricWorkerEndpoint { get; set; }
		double? Weight { get; set; }
		int? Requests { get; set; }
	}
}