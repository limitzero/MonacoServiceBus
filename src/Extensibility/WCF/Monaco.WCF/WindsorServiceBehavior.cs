using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Castle.MicroKernel;

namespace Monaco.WCF
{
	/// <summary>
	/// Class that will implement retreiving the requested service 
	/// when the endpoint is called by the client.
	/// </summary>
	public class WindsorServiceBehavior : IServiceBehavior
	{
		private readonly IKernel _kernel;

		public WindsorServiceBehavior(IKernel kernel)
		{
			_kernel = kernel;
		}

		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
		}

		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{
		
		}

		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
			{
				foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints)
				{
					if (endpointDispatcher.ContractName != "IMetadataExchange")
					{
						string contractName = endpointDispatcher.ContractName;

						ServiceEndpoint serviceEndpoint = serviceDescription.Endpoints.FirstOrDefault(e => e.Contract.Name == contractName);

						if (serviceEndpoint != null)
						{
							endpointDispatcher.DispatchRuntime.InstanceProvider = new WindsorContainerInstanceProvider(_kernel,
							                                                                                           serviceEndpoint.
							                                                                                           	Contract.ContractType);
						}
					}
				}

				
			}

		}
	}
}