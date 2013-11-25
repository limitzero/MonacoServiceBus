using System;
using System.Collections.Generic;

namespace Monaco.Configuration
{
	/// <summary>
	/// Primary contract for retrieving and storing components.
	/// </summary>
	public interface IContainer : IDisposable
	{
		void Register<T, S>() where S : class, T;
		void Register<T, S>(ContainerLifeCycle lifeCycle) where S : class, T;

		void Register<T>() where T : class;
		void Register<T>(ContainerLifeCycle lifeCycle) where T : class;

		void Register(Type contract, Type service, ContainerLifeCycle lifeCycle);
		void Register(Type contract, Type service);

		void Register(Type service);
		void Register(Type service, ContainerLifeCycle lifeCycle);

		void RegisterViaFactory<T>(Func<T> factory);

		void RegisterInstance<T>(T instance);
		void RegisterInstance(Type contract, object service);

		object Resolve(Type item);
		T Resolve<T>() where T : class;

		IEnumerable<T> ResolveAll<T>();
		object[] ResolveAll(Type type);

		T Create<T>();
	}
}