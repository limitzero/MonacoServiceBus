using Monaco.Endpoint;

namespace Monaco.Transport.Virtual
{
	public class VirtualEndpoint : BaseEndpoint
	{
		private const string _endpointAddressFormat = "vm://{unique name}";

		public VirtualEndpoint()
			: base("vm")
		{
		}

		public override void DoLocalize()
		{
			// just extract the named location from the uri by host:
			LocalizedEndpointUri = EndpointUri.Host;
		}
	}
}