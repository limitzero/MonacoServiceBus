namespace Monaco.Bus.Messages.For.Logging
{
	public class EndpointLogMessage : IAdminMessage
	{
		public string Endpoint { get; set; }
		public string Level { get; set; }
		public string Message { get; set; }
		public string Exception { get; set; }
	}
}