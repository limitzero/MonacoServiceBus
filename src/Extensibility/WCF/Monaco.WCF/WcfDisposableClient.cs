using System;
using Monaco.Bus;

namespace Monaco.WCF
{
	public class WcfDisposableClient<TContract> : DisposableAction
	{
		private readonly Action _action;

		/// <summary>
		/// Gets the instance of the concrete service as proxied by WCF 
		/// over the endpoint and binding configuration.
		/// </summary>
		public TContract Service { get; private set; }
		
		public WcfDisposableClient(Action action, TContract service)
			:base(action)
		{
			_action = action;
			Service = service;
		}

		~WcfDisposableClient()
		{
			if(!this.IsDisposed)
			{
				if(this._action != null)
				{
					this._action();
				}
			}
		}
	}
}