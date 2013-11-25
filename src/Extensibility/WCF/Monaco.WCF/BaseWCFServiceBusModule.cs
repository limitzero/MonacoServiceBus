using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Castle.MicroKernel;
using Monaco.Configuration;

namespace Monaco.WCF
{
	/// <summary>
	/// Abstract class used to host a WCF based service on the service bus for interaction 
	/// with service bus message consumers.
	/// </summary>
	/// <typeparam name="TContract">Contract for the service</typeparam>
	/// <typeparam name="TService">Concrete implementation of the service</typeparam>
	public abstract class BaseWCFServiceBusModule<TContract, TService> : IBusModule
		where TService : class , TContract
	{
		private Binding _binding;
		private EndpointAddress _endpoint;

		private static WindsorServiceHost<TContract, TService> _host;

		/// <summary>
		/// Gets the current contract associated with the WCF service.
		/// </summary>
		public TContract Contract { get; private set; }

		/// <summary>
		/// Gets or sets the protocol binding used to communicate with the WCF service.
		/// </summary>
		public Binding Binding
		{
			get { return _binding; }
			set { _binding = value; }
		}

		/// <summary>
		/// Gets or sets the uri location of the service for interaction.
		/// </summary>
		public EndpointAddress Endpoint
		{
			get { return _endpoint; }
			set { _endpoint = value; }
		}

		public WindsorServiceHost<TContract, TService> Host
		{
			get { return _host; }
			set { _host = value; }
		}

		public void Dispose()
		{
			// (4) clean up the host, as normal with WCF self-hosted instance:
			// this is called with the service bus is "stopped":
			if (_host != null)
			{
				if(_host.State == CommunicationState.Opened)
					_host.Close();
			}
			_host = null;
		}

		/// <summary>
		/// This will create a client from the binding and endpoint address using the 
		///  WCF channel factory with the settings as defined for the current WCF module.
		/// </summary>
		/// <param name="endpointConfigurationName">Optional WCF configuration endpoint name for contacting the service</param>
		/// <returns></returns>
		public WcfDisposableClient<TContract> CreateChannelFactoryClient(string endpointConfigurationName = "")
		{
			TContract service = default(TContract);
			ChannelFactory < TContract> factory = null; 

			if(string.IsNullOrEmpty(endpointConfigurationName))
			{
				factory = new ChannelFactory<TContract>(this.Binding, this.Endpoint);
				service = factory.CreateChannel();
			}
			else
			{
				factory = new ChannelFactory<TContract>(endpointConfigurationName);
				service = factory.CreateChannel();
			}

			// Doing the following will max out the size of objects to serialize 
			// when they are sent back and forth across the wire.
			foreach (var operation in factory.Endpoint.Contract.Operations)
			{
				var behavior = operation.Behaviors
					.Find<System.ServiceModel.Description.DataContractSerializerOperationBehavior>()
					as System.ServiceModel.Description.DataContractSerializerOperationBehavior;

				if (behavior != null)
				{
					behavior.MaxItemsInObjectGraph = int.MaxValue;
				}
			}

			Action close = () =>
			               	{
			               		if (factory.State == CommunicationState.Opened)
			               		{
			               			factory.Close();
			               		}
			               	};

			var client = new WcfDisposableClient<TContract>(close, service);

			return client;
		}

		/// <summary>
		/// This will set the binding and endpoint address for the hosted WCF service.
		/// </summary>
		/// <param name="binding"></param>
		/// <param name="endpointAddress"></param>
		protected void SetBindingAndEndpoint(Binding binding, EndpointAddress endpointAddress)
		{
			_binding = binding;
			_endpoint = endpointAddress;
		}

		public void Start(IContainer container)
		{
			try
			{
				// (1). let's register the embedded service host factory implementation in the container:
				container.Register(typeof(WindsorServiceHostFactory));
			}
			catch
			{
				// already there, move on:
			}

			// (2) must make sure that client and server (this piece of code) have the same 
			// bindings, nothing special here, you would do this with WCF anyway and 
			// configure any special things that you would like, no need to register the 
			// contract and service, it will be done by the host below:
			this.Configure(container);

			// (3) grab an instance of the custom self-hosted instance of the service (same as using ServiceHost in WCF)
			// and keep the host "open" when started, it will be in use as long as the bus is "started":
			var serviceHostFactory = container.Resolve<WindsorServiceHostFactory>();

			if(this._binding != null & this._endpoint != null)
				_host = serviceHostFactory.CreateSelfHost<TContract, TService>(_binding, _endpoint.Uri);
			else
			{
				_host = serviceHostFactory.CreateSelfHost<TContract, TService>();
			}
			_host.Open();

		}

		/// <summary>
		/// This is the point on the bus module where custom configuration 
		/// can be done for the WCF service that is to be hosted on the service bus.
		/// Typically the binding and endpoint uri locations are defined here for 
		/// clients to communicate with the service.
		/// </summary>
		/// <param name="container"></param>
		public abstract void Configure(IContainer container);

	}

	internal class WCFClient<TContract, TService> : System.ServiceModel.ClientBase<TService>
		where TService : class, TContract
	{
		public WCFClient(string endpointName)
			:base(endpointName)
		{
			
		}
	}
}