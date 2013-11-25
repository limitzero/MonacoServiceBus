using Monaco.Endpoint.Registrations;

namespace Monaco.Configuration.Impl
{
	public class TransportConfiguration : ITransportConfiguration
	{
		public IContainer Container { get; private set; }
		public IEndpointTransportRegistration Registration { get; private set; }

		public TransportConfiguration(IContainer container)
		{
			Container = container;
		}

		public void Register<T>() where T : class, IEndpointTransportRegistration, new()
		{
			this.Registration = new T();
		}
	}
}