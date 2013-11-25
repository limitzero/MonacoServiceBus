using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Internals.Collections;
using Monaco.Bus.MessageManagement.FaultHandling;
using Monaco.Bus.Repositories;
using Monaco.Configuration;

namespace Monaco.Bus.Persistance.FaultHandlers
{
	public class FaultHandlerConfigurationRepository : IFaultHandlerConfigurationRepository, IDisposable
	{
		private static readonly object RegistryLock = new object();
		private static IThreadSafeDictionary<Type, FaultHandlerConfiguration> registrations;
		private readonly IContainer container;
		private bool disposing;

		public FaultHandlerConfigurationRepository(IContainer container)
		{
			this.container = container;

			if (registrations == null)
			{
				registrations = new ThreadSafeDictionary<Type, FaultHandlerConfiguration>();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region IFaultHandlerConfigurationRepository Members

		public IThreadSafeDictionary<Type, FaultHandlerConfiguration> Registrations
		{
			get
			{
				if (disposing) return new ThreadSafeDictionary<Type, FaultHandlerConfiguration>();
				return registrations;
			}
		}

		public ICollection<Type> FindHandlersForMessage<TMessage>()
			where TMessage : class, IMessage, new()
		{
			return FindHandlersForMessage(new TMessage());
		}

		public ICollection<Type> FindHandlersForMessage(IMessage message)
		{
			var handlers = new List<Type>();

			List<KeyValuePair<Type, FaultHandlerConfiguration>> theRegistredHandlers =
				(from registration in Registrations
				 where registration.Key == message.GetType()
				 select registration).Distinct().ToList();

			foreach (var theRegistredConsumer in theRegistredHandlers)
			{
				handlers.AddRange(theRegistredConsumer.Value.FaultHandlers);
			}

			return handlers;
		}

		public void Register(FaultHandlerConfiguration configuration)
		{
			if (disposing) return;

			lock (RegistryLock)
			{
				if (registrations.ContainsKey(configuration.Message) == false)
				{
					registrations.Add(configuration.Message, configuration);

					foreach (Type faultHandler in configuration.FaultHandlers)
					{
						try
						{
							this.container.Register(faultHandler);
						}
						catch
						{
							// duplicate found, ignore:
							continue;
						}
					}
				}
			}
		}

		#endregion

		private void Dispose(bool disposing)
		{
			this.disposing = disposing;

			if (disposing)
			{
				if (registrations != null)
				{
					registrations.Clear();
				}
				registrations = null;
			}

		}
	}
}