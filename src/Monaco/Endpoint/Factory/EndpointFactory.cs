using System;
using System.Collections.Concurrent;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Configuration;
using Monaco.Endpoint.Registrations;

namespace Monaco.Endpoint.Factory
{
	/// <summary>
	/// Concrete factory to create and mediate sending messages out over a given transport protocol.
	/// </summary>
	public class EndpointFactory : IEndpointFactory
	{
		private bool disposed;
		private readonly IContainer container;
		private static ConcurrentDictionary<string, IEndpointTransportRegistration> RegistrationCache;
		private static ConcurrentDictionary<string, Exchange> EndpointTransportCache;

		public EndpointFactory(IContainer container)
		{
			this.container = container;

			if (EndpointTransportCache == null)
			{
				EndpointTransportCache = new ConcurrentDictionary<string, Exchange>();
			}

			if (RegistrationCache == null)
			{
				RegistrationCache = new ConcurrentDictionary<string, IEndpointTransportRegistration>();
			}
		}

		#region IEndpointFactory Members

		public void Dispose()
		{
			Disposing(true);
			GC.SuppressFinalize(this);
		}

		public Exchange Build(string endpointName)
		{
			Exchange exchange = null;

			foreach (var registration in RegistrationCache)
			{
				var uri = new Uri(string.Format("{0}://{1}/{2}", registration.Value.Scheme, 
					System.Environment.MachineName, endpointName));

				try
				{
					exchange = this.Build(uri);
					if(exchange != null) break;
				}
				catch
				{
					// try another until finished
				}

				return exchange;
			}

			return exchange;
		}

		public Exchange Build(Uri endpoint)
		{
			Exchange result = null;

			if (this.disposed) return result;

			// always get a new instance of the endpoint transport (do not cache!!!):
			result = ResolveFromRegistration(endpoint);

			if (result == null)
				throw new InvalidOperationException(
					string.Format("No endpoint to transport registration could be found for the endpoint '{0}' with scheme '{1}'." +
					              " Please make sure to place an endpoint regstration in the executable directory representing the transport that is defined by " +
					              "the endpoint uri semantics.",
					              endpoint.OriginalString,
					              endpoint.Scheme));

			// force the transport to re-connnect (w/ serialization support):
			result.Transport.SerializationProvider = this.container.Resolve<ISerializationProvider>();
			result.Transport.Reconnect();

			return result;
		}

		public void Register(IEndpointTransportRegistration endpointRegistration)
		{
			if (RegistrationCache.ContainsKey(endpointRegistration.Scheme) == false)
			{
				RegistrationCache.TryAdd(endpointRegistration.Scheme, endpointRegistration);
			}
		}

		#endregion

		private Exchange ResolveFromRegistration(Uri endpoint)
		{
			Exchange result = null;

			// need to inspect cache of registered endpoints to create the endpoint type:
			IEndpointTransportRegistration registration;
			RegistrationCache.TryGetValue(endpoint.Scheme, out registration);

			if (registration == null) return result;

			// create the endpoint from the registration:
			if (registration.Scheme == endpoint.Scheme)
			{
				registration.Container = this.container;
				result = registration.Resolve(endpoint);
				EndpointTransportCache.TryAdd(endpoint.Scheme, result);
			}

			return result;
		}

		private void Disposing(bool disposing)
		{
			if (disposing)
			{
				this.disposed = true;

				if (RegistrationCache != null)
				{
					RegistrationCache.Clear();
				}
				RegistrationCache = null;

				if (EndpointTransportCache != null)
				{
					EndpointTransportCache.Clear();
				}
				EndpointTransportCache = null;
			}
		}
	}
}