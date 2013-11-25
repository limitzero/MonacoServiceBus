using Monaco.Bus.Messages;

namespace Monaco.Bus.Events
{
	/// <summary>
	/// Event that is fired when the local endpoint is started.
	/// </summary>
	public class EndpointStarted : IAdminMessage
	{
		public string Endpoint { get; set; }
	}
}