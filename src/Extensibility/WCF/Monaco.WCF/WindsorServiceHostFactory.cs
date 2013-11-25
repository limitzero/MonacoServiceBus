using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;

namespace Monaco.WCF
{
	/// <summary>
	/// Class to create the <seealso cref="ServiceHost"/> implementation using the windsor 
	/// container for a contract and service for WCF self-hosting or IIS hosting.
	/// </summary>
	public class WindsorServiceHostFactory : ServiceHostFactory
	{
		private readonly IKernel _kernel;

		public WindsorServiceHostFactory(IKernel kernel)
		{
			_kernel = kernel;
		}

		/// <summary>
		/// This will be specifically used when the WCF service is meant to be self-hosted in a 
		/// context other than IIS. In  this case the service endpoint must be bound to the 
		/// ServiceHost in order for the requests to get routed to the actual WCF service.
		/// </summary>
		/// <typeparam name="TContract"></typeparam>
		/// <typeparam name="TService"></typeparam>
		/// <param name="binding"></param>
		/// <param name="addresses"></param>
		/// <returns></returns>
		public WindsorServiceHost<TContract, TService> CreateSelfHost<TContract, TService>(Binding binding, params Uri[] addresses)
			where TService : class, TContract
		{
			try
			{
				var service = _kernel.Resolve<TContract>();
			}
			catch
			{
				_kernel.Register(Component.For<TContract>().ImplementedBy<TService>());
			}

			return new WindsorServiceHost<TContract, TService>(_kernel, binding, addresses);
		}

		/// <summary>
		/// This will be specifically used when the WCF service is meant to be self-hosted in a 
		/// context other than IIS. In  this case the service endpoint must be bound to the 
		/// ServiceHost in order for the requests to get routed to the actual WCF service. 
		/// It will read the configuration file for the {host}{/host} section and load the 
		/// runtime service from there.
		/// </summary>
		/// <typeparam name="TContract"></typeparam>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
		public WindsorServiceHost<TContract, TService> CreateSelfHost<TContract, TService>()
			where TService : class, TContract
		{
			try
			{
				var service = _kernel.Resolve<TContract>();
			}
			catch
			{
				_kernel.Register(Component.For<TContract>().ImplementedBy<TService>());
			}

			return new WindsorServiceHost<TContract, TService>(_kernel);
		}

		/// <summary>
		/// Specifically used when the WCF service is being created via a custom ServiceHostFactory
		/// implementation where the service endpoint does not need to be re-bound to the ServiceHost
		/// and the service is being hosted by IIS.
		/// </summary>
		/// <typeparam name="TContract"></typeparam>
		/// <typeparam name="TService"></typeparam>
		/// <param name="binding"></param>
		/// <param name="addresses"></param>
		/// <returns></returns>
		public WindsorServiceHost<TContract, TService> CreateIISHost<TContract, TService>(Binding binding, params Uri[] addresses)
					where TService : class, TContract
		{
			try
			{
				var service = _kernel.Resolve<TContract>();
			}
			catch
			{
				_kernel.Register(Component.For<TContract>().ImplementedBy<TService>());
			}

			return new WindsorServiceHost<TContract, TService>(_kernel, binding, false, addresses);
		}
	}


}
