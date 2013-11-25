namespace Monaco.Endpoint
{
	public interface IRemoteEndpoint
	{
		void Receive(params IMessage[] messages);
	}
}