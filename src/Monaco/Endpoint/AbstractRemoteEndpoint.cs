namespace Monaco.Endpoint
{
	/// <summary>
	/// Abstract class that will receive messages and forward them to another 
	/// endpoint that is not on the local service bus endpoint.
	/// </summary>
	public abstract class AbstractRemoteEndpoint : IRemoteEndpoint
	{
		protected AbstractRemoteEndpoint(IServiceBus bus)
		{
			Bus = bus;
		}

		protected IServiceBus Bus { get; private set; }

		#region IRemoteEndpoint Members

		public abstract void Receive(params IMessage[] messages);

		#endregion
	}
}