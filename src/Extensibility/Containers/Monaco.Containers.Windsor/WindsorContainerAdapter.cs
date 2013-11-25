using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.Facilities.FactorySupport;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Monaco.Configuration;

namespace Monaco.Containers.Windsor
{
	/// <summary>
	/// Service bus container adapter for Windsor
	/// </summary>
	public class WindsorContainerAdapter : IContainer
	{
		private IKernel kernel;
		private bool disposed; 

		public WindsorContainerAdapter()
		{
			this.kernel = new DefaultKernel();
			this.kernel.AddFacility("factory", new FactorySupportFacility());
		}

		public void Register<T, S>() where S : class, T
		{
			this.Register<T, S>(ContainerLifeCycle.Instance);
		}

		public void Register<T, S>(ContainerLifeCycle lifeCycle) where S : class, T
		{
			if (TryResolve(typeof(T)) == true) return;

			switch (lifeCycle)
			{
				case ContainerLifeCycle.Instance:
					try
					{
						this.kernel.Register(Component.For<T>().ImplementedBy<S>().LifeStyle.Transient);
					}
					catch
					{
					}
					break;
				case ContainerLifeCycle.Singleton:
					try
					{
						this.kernel.Register(Component.For<T>().ImplementedBy<S>().LifeStyle.Singleton);
					}
					catch
					{
					}
					break;
			}
		}

		public void Register<T>() where T : class
		{
			this.Register<T>(ContainerLifeCycle.Instance);
		}

		public void Register<T>(ContainerLifeCycle lifeCycle) where T : class
		{
			if (TryResolve(typeof(T)) == true) return;

			switch (lifeCycle)
			{
				case ContainerLifeCycle.Instance:
					try
					{
						this.kernel.Register(Component.For<T>().ImplementedBy<T>().LifeStyle.Transient);
					}
					catch
					{
					}
					break;
				case ContainerLifeCycle.Singleton:
					try
					{
						this.kernel.Register(Component.For<T>().ImplementedBy<T>().LifeStyle.Singleton);
					}
					catch
					{
					}
					break;
			}
		}

		public void Register(Type contract, Type service, ContainerLifeCycle lifeCycle)
		{
			if (TryResolve(contract) == true) return;

			switch (lifeCycle)
			{
				case ContainerLifeCycle.Instance:
					try
					{
						this.kernel.Register(Component.For(contract).ImplementedBy(service).LifeStyle.Transient);
					}
					catch
					{
					}
					break;
				case ContainerLifeCycle.Singleton:
					try
					{
						this.kernel.Register(Component.For(contract).ImplementedBy(service).LifeStyle.Singleton);
					}
					catch
					{
					}
					break;
			}
		}

		public void Register(Type contract, Type service)
		{
			this.Register(contract, service, ContainerLifeCycle.Instance);
		}

		public void Register(Type service)
		{
			this.Register(service, ContainerLifeCycle.Instance);
		}

		public void Register(Type service, ContainerLifeCycle lifeCycle)
		{
			if(TryResolve(service) == true) return;

			switch (lifeCycle)
			{
				case ContainerLifeCycle.Instance:
					try
					{
						this.kernel.Register(Component.For(service).ImplementedBy(service).LifeStyle.Transient);
					}
					catch
					{
					}
					break;
				case ContainerLifeCycle.Singleton:
					try
					{
						this.kernel.Register(Component.For(service).ImplementedBy(service).LifeStyle.Singleton);
					}
					catch
					{
					}
					break;
			}
		}

		public void RegisterViaFactory<T>(Func<T> factory)
		{
			this.kernel.Register(Component.For<T>().
							UsingFactoryMethod(() => factory())
									.LifeStyle.Is(LifestyleType.Transient));
		}

		public void RegisterInstance<T>(T instance)
		{
			this.kernel.Register(Component.For<T>().Instance(instance));
		}

		public void RegisterInstance(Type contract, object service)
		{
			this.kernel.Register(Component.For(contract).Instance(service));
		}

		public object Resolve(Type item)
		{
			return this.kernel.Resolve(item);
		}

		public T Resolve<T>() where T : class
		{
			return this.Resolve(typeof(T)) as T;
		}

		public IEnumerable<T> ResolveAll<T>()
		{
			return new List<T>(this.kernel.ResolveAll<T>());
		}

		public object[] ResolveAll(Type type)
		{
			List<object> results = new List<object>();
			Array array = this.kernel.ResolveAll(type);
			foreach (var item in array)
			{
				results.Add(item);
			}
			return results.ToArray();
		}

		public T Create<T>()
		{
			return default(T);
		}

		public void Dispose()
		{
			if (this.disposed == false)
			{
				if (this.kernel != null)
				{
					this.kernel.Dispose();
				}
				this.kernel = null;
			}
			this.disposed = true;
		}

		private bool TryResolve(Type contract)
		{
			bool success = false;
		
			try
			{
				var components = this.kernel.ResolveAll<object>(contract).ToArray();
				success = true;
			}
			catch
			{
			}

			return success;
		}
	}
}