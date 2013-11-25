using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Castle.MicroKernel;

namespace Monaco.WCF
{
	/// <summary>
	/// Class that will host the contract and implementation 
	/// on a specific endpoint for invocation.
	/// </summary>
	public class WindsorServiceHost<TContract, TService> : ServiceHost
		where TService : class, TContract
	{
		private readonly IKernel _kernel;

		public WindsorServiceHost(IKernel kernel, Binding binding, params Uri[] addresses)
			: base(typeof(TService), addresses)
		{
			this.AddServiceEndpoint(typeof(TContract), binding, "");
			_kernel = kernel;
		}

		public WindsorServiceHost(IKernel kernel)
			: base(typeof(TService))
		{
			_kernel = kernel;
		}

		public WindsorServiceHost(IKernel kernel, Binding binding, bool includeServiceEndpointBinding, params Uri[] addresses)
			: base(typeof(TService), addresses)
		{
			if (includeServiceEndpointBinding == true)
			{
				this.AddServiceEndpoint(typeof (TContract), binding, "");
			}
	
			_kernel = kernel;
		}

		protected override void OnOpening()
		{
			base.OnOpening();

			if (this.Description.Behaviors.Find<WindsorServiceBehavior>() == null)
			{
				this.Description.Behaviors.Add(new WindsorServiceBehavior(this._kernel));
			}
		}

	}

}