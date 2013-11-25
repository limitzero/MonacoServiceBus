using System;
using System.Collections.Generic;
using Monaco.Configuration;
using Monaco.Endpoint.Impl.Control;
using Monaco.Extensions;

namespace Monaco.Bus
{
	public class ControlBus : IControlBus
	{
		private readonly IControlEndpoint _controlEndpoint;
		private readonly IContainer container;
		private readonly IOneWayBus _oneWayBus;

		public ControlBus(IContainer container, IOneWayBus oneWayBus)
		{
			this.container = container;
			_oneWayBus = oneWayBus;

			try
			{
				_controlEndpoint = this.container.Resolve<IControlEndpoint>();
			}
			catch
			{
				// no control endpoint:
				_controlEndpoint = null;
			}
		}

		#region IControlBus Members

		public bool IsAvailable
		{
			get { return _controlEndpoint != null; }
		}

		public void Send(IMessage[] messages)
		{
			if (_controlEndpoint != null)
			{
				_oneWayBus.Send(_controlEndpoint.Uri.ToUri(), messages);
			}
		}

		public void Send(IMessage message)
		{
			Send(new List<IMessage> {message}.ToArray());
		}

		public void Send<TMessage>(Action<TMessage> action) where TMessage : IMessage
		{
			if (_controlEndpoint != null)
			{
				_oneWayBus.Send(_controlEndpoint.Uri.ToUri(), action);
			}
		}

		#endregion
	}
}