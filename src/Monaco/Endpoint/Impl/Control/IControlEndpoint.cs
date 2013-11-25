namespace Monaco.Endpoint.Impl.Control
{
	public interface IControlEndpoint
	{
		string Uri { get; set; }
		void Receive(IServiceBus bus, params IMessage[] messages);
	}
}