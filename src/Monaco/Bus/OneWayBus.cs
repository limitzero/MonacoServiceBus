using System;
using System.Collections.Generic;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Transport;

namespace Monaco.Bus
{
	public class OneWayBus : IOneWayBus
	{
		private readonly IContainer container;

		public OneWayBus(IContainer container)
		{
			this.container = container;
		}

		#region IOneWayBus Members

		public void Send(Uri endpoint, params IMessage[] messages)
		{
			foreach (IMessage message in messages)
			{
				Send(endpoint, message);
			}
		}

		public void Send(Uri endpoint, IMessage message)
		{
			ITransport transport = GetTransport(endpoint);

			if (typeof(IEnvelope).IsAssignableFrom(message.GetType()) == false)
			{
				var envelope = new Envelope(message);
				envelope.Header.ReplyEndpoint = endpoint.OriginalString;
				transport.Send(envelope);
			}
			else
			{
				transport.Send(message as IEnvelope);
			}
		}

		public void Send<TMessage>(Uri endpoint, Action<TMessage> action)
			where TMessage : IMessage
		{
			TMessage message = default(TMessage);
			message = (TMessage)container.Resolve(typeof(TMessage));
			action(message);
			Send(endpoint, new List<IMessage> {message}.ToArray());
		}

		#endregion

		private ITransport GetTransport(Uri endpoint)
		{
			ITransport transport = null;

			var factory = container.Resolve<IEndpointFactory>();

			if (factory == null) return transport;

			Exchange exchange = factory.Build(endpoint);

			if (exchange != null)
				transport = exchange.Transport;

			return transport;
		}
	}
}