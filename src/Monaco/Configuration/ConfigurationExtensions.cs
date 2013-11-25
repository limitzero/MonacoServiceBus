using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Monaco.Extensibility.Storage.Impl.Volatile;

namespace Monaco.Configuration
{
	public static class ConfigurationExtensions
	{
		public static IContainerConfiguration UsingNullContainer(this IContainerConfiguration configuration)
		{
			configuration.Container = new NullContainer();
			return configuration;
		}

		/// <summary>
		/// This will signal to the service bus infrastructure to use the in-memory storage components for subscriptions, state machine data and time-outs.
		/// </summary>
		/// <param name="storageConfiguration"></param>
		/// <returns></returns>
		public static IStorageConfiguration UsingInMemoryStorage(this IStorageConfiguration storageConfiguration)
		{
			storageConfiguration.TimeoutsRepository = typeof (InMemoryTimeoutsRepository);
			storageConfiguration.TimeoutsRepositoryContainerLifeCycle = ContainerLifeCycle.Singleton;

			storageConfiguration.StateMachineDataRepository = typeof (InMemoryStateMachineDataRepository<>);
			storageConfiguration.StateMachineDataRepositoryContainerLifeCycle = ContainerLifeCycle.Singleton;

			storageConfiguration.SubscriptionRepository = typeof (InMemorySubscriptionRepository);
			storageConfiguration.SubscriptionRepositoryContainerLifeCycle = ContainerLifeCycle.Singleton;

			return storageConfiguration;
		}

		/// <summary>
		/// This will signal to the service bus infrastructure to use the in-memory (virtual) transport for sending and receiving messages.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static ITransportConfiguration UsingInMemory(this ITransportConfiguration configuration)
		{
			configuration.Register<Monaco.Transport.Virtual.VirtualEndpointTransportRegistration>();
			return configuration;
		}
	}

	public class NullContainer : IContainer
	{
		public void Register<T, S>() where S : class, T
		{

		}

		public void Register<T, S>(ContainerLifeCycle lifeCycle) where S : class, T
		{

		}

		public void Register<T>() where T : class
		{

		}

		public void Register<T>(ContainerLifeCycle lifeCycle) where T : class
		{

		}

		public void Register(Type contract, Type service, ContainerLifeCycle lifeCycle)
		{

		}

		public void Register(Type contract, Type service)
		{

		}

		public void Register(Type service)
		{
		
		}

		public void Register(Type service, ContainerLifeCycle lifeCycle)
		{
			
		}

		public void RegisterViaFactory<T>(Func<T> factory)
		{
			
		}

		public void RegisterViaFactory<T>(Func<T> factory, ContainerLifeCycle lifeCycle)
		{
			throw new NotImplementedException();
		}

		public void RegisterInstance<T>(T instance)
		{
			
		}

		public void RegisterInstance(Type contract, object service)
		{
			throw new NotImplementedException();
		}

		public object Resolve(Type item)
		{
			return item;
		}

		public T Resolve<T>() where T : class
		{
			return default(T);
		}

		public IEnumerable<T> ResolveAll<T>()
		{
			return new List<T>();
		}

		public object[] ResolveAll(Type type)
		{
			return new List<object>().ToArray();
		}

		public T Create<T>()
		{
			return default(T);
		}

		public void Dispose()
		{
			
		}
	}
}