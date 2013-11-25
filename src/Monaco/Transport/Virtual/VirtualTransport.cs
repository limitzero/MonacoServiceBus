using System;
using Monaco.Bus;
using Monaco.Endpoint;

namespace Monaco.Transport.Virtual
{
	/// <summary>
	/// In-memory transport for testing without interaction to a
	/// transport with persistance mechanism. All of the configured
	/// endpoints will be hosted on a single bus instance  
	/// to take the place of a physical message store per endpoint.
	/// Addressing scheme for transport: vm://{unique endpoint name}
	/// </summary>
	public class VirtualTransport : BaseTransport<object>
	{
		private bool isDisconnected;

		public VirtualTransport(VirtualEndpoint endpoint)
			: base(endpoint)
		{
		}

		public override void Connect()
		{
			Endpoint.Localize();
			CreateEndpoint(Endpoint.LocalizedEndpointUri);
			this.isDisconnected = false;
		}

		public override void Disconnect()
		{
			this.isDisconnected = true;
		}

		public override IEnvelope DoReceive(TimeSpan timeout)
		{
			IEnvelope envelope = null;

			if (this.isDisconnected) return envelope;

			PeekMessage(Endpoint.LocalizedEndpointUri, out envelope);

			return envelope;
		}

		public override void DoSend(IEnvelope envelope)
		{
			DoSend(Endpoint, envelope);
		}

		public override void DoSend(IEndpoint endpoint, IEnvelope envelope)
		{
			endpoint.Localize();

			string location = endpoint.LocalizedEndpointUri;

			if (isDisconnected) return;

			if (string.IsNullOrEmpty(location) == false)
			{
				CreateEndpoint(location);
			}

			VirtualTransportStorage.GetInstance().Enqueue(location, envelope);
		}

		private bool PeekMessage(string endpoint, out IEnvelope envelope)
		{
			envelope = null;

			if (Disposed) return false;

			envelope = VirtualTransportStorage.GetInstance().Dequeue(endpoint);

			return (envelope != null);
		}

		private static void CreateEndpoint(string endpoint)
		{
			VirtualTransportStorage.GetInstance().Initialize(endpoint);
		}
	}
}