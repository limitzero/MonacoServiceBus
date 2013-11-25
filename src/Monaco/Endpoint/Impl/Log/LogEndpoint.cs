using Monaco.Extensions;

namespace Monaco.Endpoint.Impl.Log
{
	/// <summary>
	/// Remote endpoint to forward log messages.
	/// </summary>
	public class LogEndpoint : AbstractRemoteEndpoint
	{
		public LogEndpoint(IServiceBus bus)
			: base(bus)
		{
		}

		public string Uri { get; set; }

		public override void Receive(params IMessage[] messages)
		{
			foreach (IMessage message in messages)
			{
				Bus.Send(Uri.ToUri(), message);
			}
		}
	}
}