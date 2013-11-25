using System;

namespace Monaco.Distributor.Internals.Fabric.Impl
{
	public class FabricWorkerConfiguration : IFabricWorkerConfiguration
	{
		public Uri FabricWorkerEndpoint { get; set; }
		public double? Weight { get; set; }
		public int? Requests { get; set; }
	}
}