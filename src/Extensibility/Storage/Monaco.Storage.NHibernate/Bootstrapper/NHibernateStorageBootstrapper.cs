using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core;
using Castle.Facilities.FactorySupport;
using Castle.MicroKernel.Registration;
using Monaco.Configuration;
using Monaco.Configuration.Bootstrapper.Roles;
using Monaco.Configuration.Endpoint;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Storage.NHibernate.Sagas;
using Monaco.Storage.NHibernate.Subscriptions;
using Monaco.Storage.NHibernate.Timeouts;
using NHibernate;

namespace Monaco.Storage.NHibernate.Bootstrapper
{
    public class NHibernateStorageBootstrapper : BaseBusStorageProviderBootstrapper
    {
    	public NHibernateStorageBootstrapper()
    	{
			IsActive = false;
    	}

        public override void Configure()
        {
            ConfigureSubscriptionsStorage();

            ConfigureSagaStorage();

            ConfigureTimeoutsStorage();

			//Container.AddComponent("nhibernate.connection.factory",
			//                    typeof(NHibernateConnectionFactory),
			//                    typeof(NHibernateConnectionFactory));

			//// configure the main NHibernate configuration (only once)
			//// and trigger the connection to the session from the static 
			//// method on the connection factory object:
			//var configuration = new global::NHibernate.Cfg.Configuration();
			//configuration.Configure();

			//configuration.AddAssembly(this.GetType().Assembly);

			//ScanEndpointConfigurationsForMappings(configuration);

			//var sessionFactory = configuration.BuildSessionFactory();

			//Container.AddFacility("factory", new FactorySupportFacility());

			//Container.Register(Component.For<global::NHibernate.Cfg.Configuration>().Instance(configuration));

			//Container.Register(Component.For<ISessionFactory>().Instance(sessionFactory));

			//// use the factory method on the class to instantiate an on-demand session:
			//Container.Register(Component.For<ISession>().
			//                    UsingFactoryMethod(() =>
			//                                       Container.Resolve<NHibernateConnectionFactory>().GetCurrentSession())
			//                    .LifeStyle.Is(LifestyleType.Transient));

			//// register the schema manager in case we need to create the data schema (testing only):
			//Container.Register(Component.For<INHibernateSchemaManager>()
			//                    .ImplementedBy<NHibernateSchemaManager>());
        }

		/// <summary>
		/// This will allow for any endpoints to participate with NHibernate for entity persistance and retrieval.
		/// </summary>
		/// <param name="configuration"></param>
    	private static void ScanEndpointConfigurationsForMappings(global::NHibernate.Cfg.Configuration configuration)
    	{
    		var files = Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "*.dll");

    		foreach (var file in files)
    		{
    			try
    			{
    				Assembly asm = Assembly.LoadFile(file);

    				var endpointConfiguration = (from match in asm.GetTypes()
    				                             where
    				                             	match.IsClass == true
    				                             	&& match.IsAbstract == false
    				                             	&& typeof (BaseEndpointConfiguration).IsAssignableFrom(match)
    				                             select match).FirstOrDefault();

					if(endpointConfiguration != null)
					{
						configuration.AddAssembly(endpointConfiguration.Assembly);
					}
    			}
    			catch
    			{
					continue;
    			}
    		}
    	}

    	private void ConfigureSagaStorage()
        {
			//Container.Register(Component.For(typeof (IStateMachineDataRepository<>))
			//                    .ImplementedBy(typeof (NHibernateStateMachineDataRepository<>)));
        }

        private void ConfigureTimeoutsStorage()
        {
			this.RegisterComponent<ITimeoutsRepository, NHibernateTimeoutsRepository>();
        }

        private void ConfigureSubscriptionsStorage()
        {
			this.RegisterComponent<ISubscriptionRepository, NHibernateSubscriptionRepository>();
        }

		private void RegisterComponent<TContract, TService>() where TService : class, TContract
		{
			// try to find the component first, if it is there, remove and add the implementation:
			this.RegisterComponent(typeof(TContract), typeof(TService));
		}

		private void RegisterComponent(Type contract, Type service)
		{
			// try to find the component first, if it is there, remove and add the implementation:
			try
			{
				var component = Container.Resolve(contract);
				//Container.RemoveComponent(contract.Name);
			}
			catch
			{
				
			}
			finally
			{
				//Container.AddComponent(contract.Name, contract, service);
			}
		}

    }
}