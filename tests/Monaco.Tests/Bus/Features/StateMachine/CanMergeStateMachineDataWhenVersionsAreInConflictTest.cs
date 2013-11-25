using System;
using System.Linq;
using System.Threading;
using Monaco.Configuration;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.StateMachine;
using Xunit;

namespace Monaco.Tests.Bus.Features.StateMachine
{
	public class MergeStateMachineDataEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => e.MapMessages<CanMergeStateMachineDataWhenVersionsAreInConflictTest.MergeStateMachine>()
								.SupportsTransactions(true)
				                .ConfigureStateMachineDataMergers(this.GetType().Assembly));
		}
	}

	public class CanMergeStateMachineDataWhenVersionsAreInConflictTest : IDisposable
	{
		public static bool isDataMerged;
		public static ManualResetEvent wait;
		private MonacoConfiguration configuration;

		public CanMergeStateMachineDataWhenVersionsAreInConflictTest()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<MergeStateMachineDataEndpointConfig>(@"saga.bus.config");
			wait = new ManualResetEvent(false);
		}

		public void Dispose()
		{
			if(wait != null)
			{
				wait.Close();
				wait.Dispose();
			}
			wait = null; 

			if(configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;
		}

		[Fact]
		public void can_send_message_to_state_machine_and_fire_data_merge_for_updated_instance_version()
		{
			string accountNumber = Guid.NewGuid().ToString();

			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				// start the state machine:
				var message1 = bus.CreateMessage<MergeMessage1>( m=> m.AccountNumber = accountNumber);
				bus.Publish(message1);
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));

				// change the version of the stored running data to force the merge process 
				// (has to be a version less than the one defined in the instance data):
				var repository = configuration.Container.Resolve<IStateMachineDataRepository<MergeStateMachineData>>();
				var data = repository.FindAll().FirstOrDefault();
				data.Version += -1; /* versions less than the stated one will force a merge (simulate an upgrade) */
				repository.Save(data);

				// send a new message into the state machine that was started (this forces the merge for message #2
				// since the state data is behind by one version):
				var message2 = bus.CreateMessage<MergeMessage2>(m => m.AccountNumber = accountNumber);
				bus.Publish(message2);
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5));

				// make sure the merge actually happened:
				data = repository.FindAll().FirstOrDefault();
				Assert.True(isDataMerged, "The data merger was not invoked");
				Assert.True(data.IsUpdated, "The instance data was not merged.");
			}
		}

		public interface IMergeableMessage : IMessage
		{
			string AccountNumber { get; set; }			
		}

		public interface MergeMessage1 : IMergeableMessage {}

		public interface MergeMessage2 : IMergeableMessage {}
	
		public class MergeStateMachineData : IStateMachineData
		{
			public Guid Id { get; set; }
			public string State { get; set; }
			public int Version { get; set; }
			public string AccountNumber { get; set; }
			public bool IsUpdated { get; set; }

			public MergeStateMachineData()
			{
				// set the version of the instance data:
				this.Version = 5;
			}
		}

		public class MergeStateMachine : 
			SagaStateMachine<MergeStateMachineData>, 
			StartedBy<MergeMessage1>, 
			OrchestratedBy<MergeMessage2>
		{
			public Event<MergeMessage1> FirstMessageArrives { get; set; }
			public Event<MergeMessage2> SecondMessageArrives { get; set; }

			public State WaitingForSecondMessageToArrive { get; set; }

			public override void Define()
			{
				Initially(
					When(FirstMessageArrives)
						.Do( (message)=>
						     	{
						     		this.Data.AccountNumber = message.AccountNumber;
						     	})
						.TransitionTo(WaitingForSecondMessageToArrive)
						);

				While(WaitingForSecondMessageToArrive,
					When(SecondMessageArrives)
					   .Do((message) =>
					       	{
					       		/* nothing, merge should happen before this point and new instance data is ready to use */
					       	}));

			}

			public override void ConfigureHowToFindStateMachineInstanceDataFromMessages()
			{
				CorrelateMessageToStateMachineData<MergeMessage1>(s=>s.AccountNumber, m=>m.AccountNumber);
				CorrelateMessageToStateMachineData<MergeMessage2>(s => s.AccountNumber, m => m.AccountNumber);
			}

			public void Consume(MergeMessage1 message)
			{
				
			}

			public void Consume(MergeMessage2 message)
			{
				
			}
		}

		// if the state data will need to be extended due to a message carrying more 
		// data for persistance, this is how you do it, merge on a per message basis:
		public class MergeStateMachineDataMerger :
		IStateMachineDataMerger<MergeStateMachineData, MergeMessage2>
		{
			public MergeStateMachineData Merge(
				MergeStateMachineData currentStateMachineData, /* created by runtime  (contains most recent version, but data is empty!!!)*/
				MergeStateMachineData retreivedStateMachineData, /* retreived from persistance store  (contains previous version and data)*/
				MergeMessage2 stateMachineMessage) /* message where the merge was triggered (second call)*/
			{
				isDataMerged = true;

				/* call a database, web service, or something else that returns relatively quickly to aid in the merge */

				var result =  new MergeStateMachineData
				{
					/* no need to keep track of the instance id, the runtime will do this for you if you return a non-null merge result */
					/* no need to keep track of the right version, the runtime will do this for you if you return a non-null merge result */
					State = retreivedStateMachineData.State,
					IsUpdated = isDataMerged,
					AccountNumber = retreivedStateMachineData.AccountNumber,
				};

				return result;
			}
		}


	}
}