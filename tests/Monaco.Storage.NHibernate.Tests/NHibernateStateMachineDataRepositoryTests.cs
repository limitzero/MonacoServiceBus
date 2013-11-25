using System;
using Monaco.Bus.Internals.Reflection;
using Monaco.Configuration;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.StateMachine;
using Xunit;

namespace Monaco.Storage.NHibernate.Tests
{
	public class NHibernateStateMachineDataEndpointConfiguration : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingNHibernate(h=> h.WithConfigurationFile(@"hibernate.cfg.xml")
					 .WithEntitiesFromAssembly(this.GetType().Assembly)
				     .DropAndCreateSchema()))
				.WithTransport(t => t.UsingInMemory())
				.WithEndpoint(e => e.MapAll(this.GetType().Assembly));
		}
	}

	// Note: It is a good practice to follow NHibernate rules for creating the *.hbm.xml file for storing the state machine data just like any other persistant entity 
	public class NHibernateStateMachineDataRepositoryTests : IDisposable
	{
		private MonacoConfiguration configuration;
		private readonly Guid correlationId;

		public NHibernateStateMachineDataRepositoryTests()
		{
			configuration =  MonacoConfiguration
				.BootFromEndpoint<NHibernateStateMachineDataEndpointConfiguration>(@"sample.config");
			correlationId = CombGuid.NewGuid();
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;
		}

		[Fact]
		public void can_use_repository_to_save_state_machine_instance_data()
		{
			var persisterType = typeof(IStateMachineDataRepository<>).MakeGenericType(typeof(MyStateMachineData));
			var persister = configuration.Container.Resolve(persisterType);

			var stateMachineData = new MyStateMachineData { Id = correlationId, AccountNumber = "12334545" };
			var reflection = configuration.Container.Resolve<IReflection>();

			reflection.InvokeStateMachineDataRepositorySave(persister, stateMachineData);

			var fromDb = reflection.InvokeStateMachineDataRepositoryFindById(persister, stateMachineData.Id);

			Assert.Equal(correlationId, fromDb.Id);
		}

		private TStateMachine CreateStateMachine<TStateMachine, TStateMachineData>(Guid instanceId)
			where TStateMachine : SagaStateMachine<TStateMachineData>
			where TStateMachineData : class, IStateMachineData, new()
		{
			var stateMachine = configuration.Container.Resolve<TStateMachine>();
			stateMachine.InstanceId = instanceId;
			stateMachine.Data = new TStateMachineData();
			stateMachine.Data.Id = instanceId;

			stateMachine.CurrentState = new State("Test");
			stateMachine.Data.State = stateMachine.CurrentState.Name;

			return stateMachine;
		}

		private IStateMachineData GetStateMachineData(SagaStateMachine stateMachine)
		{
			var reflection = configuration.Container.Resolve<IReflection>();
			return reflection.GetProperty<IStateMachineData>(stateMachine, "Data");
		}
	}
}