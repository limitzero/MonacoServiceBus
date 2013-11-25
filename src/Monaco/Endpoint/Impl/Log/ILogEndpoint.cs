namespace Monaco.Endpoint.Impl.Log
{
	public interface ILogEndpoint
	{
		string Uri { get; set; }
		void Receive(params IMessage[] messages);
	}
}