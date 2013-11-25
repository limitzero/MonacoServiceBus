using System;
using System.Collections.Generic;
using System.Linq;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Configuration;
using Monaco.Extensibility.Storage.Timeouts;
using Xunit;

namespace Monaco.Storage.NHibernate.Tests
{
	// All timeouts reflect the current bus instance endpoint that 
	// generated the request when they are issued by the message consumer 
	// to the service bus. You can query for timeouts on differing
	// endpoints by the overload on ITimeoutsRepository.FindAll(..)

	public class NHibernateTimeoutsRepositoryEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingNHibernate(h => h.WithConfigurationFile(@"hibernate.cfg.xml")
					 .WithEntitiesFromAssembly(this.GetType().Assembly)
					 .DropAndCreateSchema()))
				.WithTransport(t => t.UsingInMemory())
				.WithEndpoint(e => e.SupportsTransactions(false));
		}
	}

	public class NHibernateTimeoutsRepositoryTests : IDisposable
	{
		private MonacoConfiguration configuration;

		public NHibernateTimeoutsRepositoryTests()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<NHibernateTimeoutsRepositoryEndpointConfiguration> (@"sample.config");
		}

		public void Dispose()
		{
			if(configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;
		}

		[Fact]
		public void can_use_repository_to_add_and_retrieve_a_timeout_instance()
		{
			var repository = configuration.Container.Resolve<ITimeoutsRepository>();
			var endpoint = "vm://my.timeout";

			MyMessage message = new MyMessage();
			ScheduleTimeout timeout = new ScheduleTimeout(TimeSpan.FromSeconds(10), message);
			timeout.Endpoint = endpoint;

			repository.Add(timeout);

			// find the timeouts for the transport endpoint (this will usually be the service bus endpoint)::
			var fromDb = repository.FindAll(endpoint);
			List<ScheduleTimeout> timeouts = new List<ScheduleTimeout>(fromDb);

			Assert.Equal(1, fromDb.Count);
			Assert.Equal(timeout.Id, timeouts[0].Id);
		}

		[Fact]
		public void can_use_repository_to_remove_an_active_instance_of_timeout()
		{
			var repository = configuration.Container.Resolve<ITimeoutsRepository>();
			var endpoint = "vm://my.timeout";

			MyMessage message = new MyMessage();
			ScheduleTimeout timeout = new ScheduleTimeout(TimeSpan.FromSeconds(10), message);
			timeout.Endpoint = endpoint;

			repository.Add(timeout);

			var fromDb = repository.FindAll(endpoint);
			
			Assert.Equal(1, fromDb.Count);
			Assert.Equal(timeout.Id, fromDb.First().Id);

			// remove the timeout:
			repository.Remove(timeout);

			fromDb = repository.FindAll(endpoint);
			Assert.Equal(0, fromDb.Count);
		}
	}
}