using Monaco.Endpoint.Registrations;

namespace Monaco.Configuration
{
	public interface ITransportConfiguration
	{
		IContainer Container { get; }
		IEndpointTransportRegistration Registration { get; }
		void Register<T>() where T : class, IEndpointTransportRegistration, new();
	}
}