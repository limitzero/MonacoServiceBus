using Monaco.Extensions;

namespace Monaco.Endpoint.Impl.Control
{
	public class ControlEndpoint : IControlEndpoint
	{
		#region IControlEndpoint Members

		public string Uri { get; set; }

		public void Receive(IServiceBus bus, params IMessage[] messages)
		{
			foreach (IMessage message in messages)
			{
				bus.Send(Uri.ToUri(), message);
			}
		}

		#endregion
	}
}