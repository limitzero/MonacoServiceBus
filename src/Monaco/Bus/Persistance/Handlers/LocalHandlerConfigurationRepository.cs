using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.Repositories;
using Monaco.Configuration;
using Monaco.Configuration.Registration;

namespace Monaco.Bus.Persistance.Handlers
{
	public class LocalHandlerConfigurationRepository : IHandlerConfigurationRepository, IDisposable
	{
		private static readonly object _registry_lock = new object();
		private static IThreadSafeDictionary<Type, HandlerConfiguration> _registrations;
		private bool _disposing;

		public LocalHandlerConfigurationRepository()
		{
			if (_registrations == null)
			{
				_registrations = new ThreadSafeDictionary<Type, HandlerConfiguration>();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region IHandlerConfigurationRepository Members

		public IThreadSafeDictionary<Type, HandlerConfiguration> Registrations
		{
			get
			{
				if (_disposing) return new ThreadSafeDictionary<Type, HandlerConfiguration>();
				return _registrations;
			}
		}

		public ICollection<Type> FindConsumersForMessage<TMessage>() where TMessage : class, IMessage, new()
		{
			return FindConsumersForMessage(new TMessage());
		}

		public ICollection<Type> FindConsumersForMessage(object message)
		{
			var consumers = new List<Type>();

			List<KeyValuePair<Type, HandlerConfiguration>> theRegistredConsumers = (from registration in Registrations
			                                                                        where registration.Key == message.GetType()
			                                                                        select registration).Distinct().ToList();

			foreach (var theRegistredConsumer in theRegistredConsumers)
			{
				consumers.AddRange(theRegistredConsumer.Value.Consumers);
			}

			return consumers;
		}

		public void Register(HandlerConfiguration configuration)
		{
			if (_disposing) return;

			lock (_registry_lock)
			{
				_registrations.Add(configuration.Message, configuration);
			}
		}

		#endregion

		private void Dispose(bool disposing)
		{
			_disposing = disposing;

			if (disposing)
			{
				_registrations.Clear();
			}
			_registrations = null;
		}
	}
}