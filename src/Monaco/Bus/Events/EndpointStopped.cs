using Monaco.Bus.Messages;

namespace Monaco.Bus.Events
{
	/// <summary>
	/// Event that is fired when the local endpoint is stopped.
	/// </summary>
	public class EndpointStopped : IAdminMessage
	{
		public string Endpoint { get; set; }
	}
}