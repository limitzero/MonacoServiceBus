using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Monaco.Bus.Internals.Reflection;
using Monaco.Bus.Messages.For.Recovery;
using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Impl.Bus;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.StateMachine;
using Xunit;

namespace Monaco.Tests.Bus.Features.StateMachine
{
	public class SagaStateMachineEndpointConfig : ICanConfigureEndpoint
	{
		public void Configure(IConfiguration configuration)
		{
			configuration
				.WithContainer(c => c.UsingWindsor())
				.WithStorage(s => s.UsingInMemoryStorage())
				.WithTransport(t => t.UsingMsmq())
				.WithEndpoint(e => 
					// scanning cannot find nested types !!!
					e.MapMessages<SagaStateMachineTests.LocalStateMachine>()
				  .ConfigureStateMachineDataMergers(this.GetType().Assembly));
		}
	}
	
	public class SagaStateMachineTests : IDisposable
	{
		public static Guid instanceId = Guid.Empty;
		public static List<object> receivedMessages;
		public static ManualResetEvent wait;
		private MonacoConfiguration configuration;
		public static bool isReceivedAfterDelay;
		public static bool isDataMerged;

		private string accountNumber = "1234567890";

		public SagaStateMachineTests()
		{
			configuration = MonacoConfiguration
				.BootFromEndpoint<SagaStateMachineEndpointConfig>(@"saga.bus.config");

			wait = new ManualResetEvent(false);
			receivedMessages = new List<object>();
			instanceId = Guid.Empty;
		}

		public void Dispose()
		{
			if (configuration != null)
			{
				configuration.Dispose();
			}
			configuration = null;

			if (wait != null)
			{
				wait.Close();
				wait = null;
			}
		}

		[Fact]
		public void can_publish_message_to_statemachine()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				bus.Publish<LocalSagaStateMachineMessage>(m=> m.AccountNumber = this.accountNumber);

				wait.WaitOne(TimeSpan.FromSeconds(10));

				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage>());
			}
		}

		[Fact]
		public void can_dispatch_message_to_start_statemachine_and_have_data_be_correlated_to_the_statemachine_instance()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				bus.Publish<LocalSagaStateMachineMessage>(m => m.AccountNumber = this.accountNumber);

				wait.WaitOne(TimeSpan.FromSeconds(10));

				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage>());

				var data = this.GetStateMachineData<LocalStateMachineStateMachineData>(this.accountNumber);
				Assert.Equal(this.accountNumber, data.AccountNumber);
			}
		}

		[Fact]
		public void can_send_message_to_already_started_statemachine_and_have_the_saga_data_be_correlated_to_the_statemachine_instance()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				// start the saga:
				bus.Publish<LocalSagaStateMachineMessage>(m => m.AccountNumber = this.accountNumber);

				wait.WaitOne(TimeSpan.FromSeconds(10));
				wait.Reset();

				Assert.NotEqual(Guid.Empty, instanceId);
				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage>(), "First message not received");

				// send a new message into the saga that was started:
				bus.Publish<LocalSagaStateMachineMessage2>(m => m.AccountNumber = this.accountNumber);
				wait.WaitOne(TimeSpan.FromSeconds(10));

				//Assert.True(IsMessageReceived<LocalSagaStateMachineMessage2>(), "Second message not received.");

				var data = this.GetStateMachineData<LocalStateMachineStateMachineData>(this.accountNumber);
				//Assert.Equal(this._accountNumber, data.AccountNumber);
			}
		}

		[Fact]
		public void can_send_message_to_already_started_saga_and_send_another_correlated_message_to_complete_and_remove_instance()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				// start the saga:
				bus.Publish<LocalSagaStateMachineMessage>(m => m.AccountNumber = this.accountNumber);

				wait.WaitOne(TimeSpan.FromSeconds(5));
				wait.Reset();

				Assert.NotEqual(Guid.Empty, instanceId);
				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage>());

				// send a new message into the saga that was started:
				bus.Publish<LocalSagaStateMachineMessage2>(m => m.AccountNumber = this.accountNumber);
				wait.WaitOne(TimeSpan.FromSeconds(5));
				wait.Reset();

				// send a  message into the saga and trigger it to complete:
				bus.Publish<LocalSagaStateMachineMessage3>(m => m.AccountNumber = this.accountNumber);
				wait.WaitOne(TimeSpan.FromSeconds(10));
	
				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage3>());
			}
		}

		[Fact]
		public void can_send_message_to_state_machine_to_start_and_send_subsequent_message_to_trigger_the_also_condtion()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				bus.Publish<LocalSagaStateMachineMessage>(m => m.AccountNumber = this.accountNumber);

				wait.WaitOne(TimeSpan.FromSeconds(5));

				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage>());
			}
		}

		[Fact]
		public void can_send_message_to_state_machine_to_check_for_correlation_and_push_non_correlated_message_to_error_queue()
		{
			var correlationId = CombGuid.NewGuid();
			var accountNumber = "12345678890";

			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				var errorEndpoint = configuration.Container.Resolve<IServiceBusErrorEndpoint>();
				var endpointFactory = configuration.Container.Resolve<IEndpointFactory>();
				var endpointTransport = endpointFactory.Build(errorEndpoint.Endpoint);
				var transport = endpointTransport.Transport;
				transport.IsRecoverable = false;  // force the clearing of the endpoint storage:

				bus.Start();

				// start the saga:
				bus.Publish<LocalSagaStateMachineMessage>(m =>
															{
																m.AccountNumber = accountNumber;
															});

				// send message to fail correlation (account number do not match):
				bus.Send<NonCorrelatedStateMachineMessage>(m =>
															{
																m.CorrelationId = correlationId;
																m.AccountNumber = Guid.NewGuid().ToString();
															});

				// wait for the bus to finish handling the non-correlated message:
				System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));

				// all exceptions are wrapped in a recovery message:
				var envelope = transport.Receive(TimeSpan.FromSeconds(5));
				var recoveryMessage = envelope.Body.Payload.First() as RecoveryMessage;

				Assert.IsType<NonCorrelatedStateMachineMessage>(recoveryMessage.Envelope.Body.Payload.First());
			}
		}

		[Fact]
		public void can_send_message_to_state_machine_and_fire_data_merge_for_updated_instance_version()
		{
			using (IServiceBus bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				// start the saga:
				bus.Publish<LocalSagaStateMachineMessage>(m => m.AccountNumber = this.accountNumber);

				wait.WaitOne(TimeSpan.FromSeconds(10));
				wait.Reset();

				Assert.NotEqual(Guid.Empty, instanceId);
				Assert.True(IsMessageReceived<LocalSagaStateMachineMessage>());

				// change the version of the current running data to force the merge process 
				// (has to be a version less than the one defined in the instance data):
				var repository = configuration.Container.Resolve<IStateMachineDataRepository<LocalStateMachineStateMachineData>>();
				var data = repository.FindAll().FirstOrDefault();
				data.Version = -100; /* versions always start at zero */
				repository.Save(data);

				// send a new message into the saga that was started:
				bus.Publish<LocalSagaStateMachineMessage2>(m => m.AccountNumber = this.accountNumber);
				wait.WaitOne(TimeSpan.FromSeconds(10));

				// make sure the merge happened:
				Assert.True(isDataMerged);	
			}
		}

		[Fact(Skip = "This suspending of the state machine is not needed")]
		public void can_send_message_to_start_saga_and_restart_the_saga_state_machine_after_suspend_period()
		{
			using (var bus = configuration.Container.Resolve<IServiceBus>())
			{
				bus.Start();

				// start the saga:
				bus.Publish<LocalSagaStateMachineMessage>(m =>
				{
					m.AccountNumber = this.accountNumber;
				});

				wait.WaitOne(TimeSpan.FromSeconds(10));
				Assert.True(isReceivedAfterDelay);
			}
		}

		private TData GetStateMachineData<TData>(string accountNumber)
			where TData : class, IStateMachineData
		{
			var persisterType = typeof(IStateMachineDataRepository<>)
				.MakeGenericType(typeof(TData));
			var persister = configuration.Container.Resolve(persisterType);

			var reflection = configuration.Container.Resolve<IReflection>();
			var instances = reflection.InvokeStateMachineDataRepositoryFindAll(persister);

			var instance = (from item in instances
			                let data = item as TData
			                from property in data.GetType().GetProperties()
			                where property.GetValue(item, null).Equals(accountNumber)
			                select data).FirstOrDefault();

			return instance;
		}

		private static bool IsMessageReceived<TMESSAGE>() where TMESSAGE : IMessage
		{
			var isReceived = false;

			isReceived = receivedMessages.Exists(x => x.GetType() == typeof(TMESSAGE));

			return isReceived;
		}

		public class NonCorrelatedStateMachineMessage : IMessage
		{
			public Guid CorrelationId { get; set; }
			public string AccountNumber { get; set; }
		}

		public class SuspendTestStateMachineStateMachineData : IStateMachineData
		{
			public Guid Id { get; set; }
			public string State { get; set; }
			public int Version { get; set; }
		}

		//public class SuspendTestStateMachine
		//    : SagaStateMachine<SuspendTestStateMachineStateMachineData>,
		//    StartedBy<LocalSagaStateMachineMessage>,
		//    OrchestratedBy<LocalSagaStateMachineMessage2>
		//{

		//    public Event<LocalSagaStateMachineMessage> Started { get; set; }
		//    public Event<LocalSagaStateMachineMessage2> RevivedFromSuspend { get; set; }

		//    public State SagaSuspended { get; set; }

		//    public override void Define()
		//    {
		//        Initially(
		//            When(Started)
		//                .Delay<LocalSagaStateMachineMessage2>(5.Seconds().FromNow(), 
		//                  (r,s)=>
		//                    {
		//                        s.AccountNumber = r.AccountNumber;
		//                    })
		//                .TransitionTo(SagaSuspended)
		//            );

		//        While(SagaSuspended,
		//            When(RevivedFromSuspend)
		//                .Do((receivedMessage) => { 
		//                    isReceivedAfterDelay = true;
		//                    _wait.Set();
		//                })
		//                .Complete()
		//            );
		//    }

		//    public void Consume(LocalSagaStateMachineMessage message)
		//    {
		//    }

		//    public void Consume(LocalSagaStateMachineMessage2 message)
		//    {
		//    }

		//    public override void OnSuspendCompleted(object state)
		//    {
				
		//    }
		//}

		public class LocalStateMachineDataMerger : 
			IStateMachineDataMerger<LocalStateMachineStateMachineData, LocalSagaStateMachineMessage2>
		{
			public LocalStateMachineStateMachineData Merge(
				LocalStateMachineStateMachineData currentStateMachineData, /* created by runtime */
				LocalStateMachineStateMachineData retreivedStateMachineData, /* retreived from persistance store */
				LocalSagaStateMachineMessage2 stateMachineMessage) /* message where the merge was triggered */
			{
				isDataMerged = true;
				return new LocalStateMachineStateMachineData
				       	{
				       		Id = retreivedStateMachineData.Id,
				       		State = retreivedStateMachineData.State,
							IsVersionUpdated = isDataMerged,
				       		AccountNumber = retreivedStateMachineData.AccountNumber,
				       		Version = currentStateMachineData.Version /* always set to the current version */
				       	};
			}
		}

		public class LocalStateMachine :
			SagaStateMachine<LocalStateMachineStateMachineData>,
			StartedBy<LocalSagaStateMachineMessage>,
			OrchestratedBy<LocalSagaStateMachineMessage2>,
			OrchestratedBy<LocalSagaStateMachineMessage3>,
			OrchestratedBy<LocalSagaStateMachineMessage4>,
			OrchestratedBy<NonCorrelatedStateMachineMessage>
		{
			// events: 
			public Event<LocalSagaStateMachineMessage> Started { get; set; }
			public Event<LocalSagaStateMachineMessage2> SecondMessageReceived { get; set; }
			public Event<LocalSagaStateMachineMessage3> ThirdMessageReceived { get; set; }
			public Event<LocalSagaStateMachineMessage4> AlsoConditionTriggered { get; set; }
			public Event<NonCorrelatedStateMachineMessage> NonCorrelatedMessageArrived { get; set; }

			// states:
			public State WaitingForSecondMessage { get; set; }
			public State WaitingForThirdMessage { get; set; }
			public State AttemptingToCorrelateMessage { get; set; }

			public void Consume(LocalSagaStateMachineMessage message)
			{

			}

			public void Consume(LocalSagaStateMachineMessage2 message)
			{

			}

			public void Consume(LocalSagaStateMachineMessage3 message)
			{
			}

			public void Consume(LocalSagaStateMachineMessage4 message)
			{
			}

			public void Consume(NonCorrelatedStateMachineMessage message)
			{

			}

			public override void ConfigureHowToFindStateMachineInstanceDataFromMessages()
			{
				CorrelateMessageToStateMachineData<LocalSagaStateMachineMessage>(s => s.AccountNumber, m => m.AccountNumber);
				CorrelateMessageToStateMachineData<LocalSagaStateMachineMessage2>(s => s.AccountNumber, m => m.AccountNumber);
				CorrelateMessageToStateMachineData<LocalSagaStateMachineMessage3>(s => s.AccountNumber, m => m.AccountNumber);
				CorrelateMessageToStateMachineData<LocalSagaStateMachineMessage4>(s => s.AccountNumber, m => m.AccountNumber);
			}

			public override void Define()
			{
				Initially(
					When(Started)
					.Do((sagaMessage) =>
							{
								instanceId = InstanceId;
								Data.AccountNumber = sagaMessage.AccountNumber;
								receivedMessages.Add(sagaMessage);
								wait.Set();
							}, "Receive the initial message and add it to the internal collection for testing :)")
					.TransitionTo(WaitingForSecondMessage)
					);

				While(WaitingForSecondMessage,
					When(SecondMessageReceived)
						.Do((sagaMessage) =>
						    	{
									receivedMessages.Add(sagaMessage);
									wait.Set();
								})
						.TransitionTo(WaitingForThirdMessage)
						);

				While(WaitingForThirdMessage,
					When(ThirdMessageReceived)
						.Do((sagaMessage) =>
						    	{
						    		receivedMessages.Add(sagaMessage);
						    		wait.Set();
						    	})
						.Complete()
						);

				While(AttemptingToCorrelateMessage,
					  When(NonCorrelatedMessageArrived)
						.CorrelatedBy((message) =>
							message.AccountNumber == this.Data.AccountNumber)
					.Do((message) => { }, "no-op")
				);

				Also(
					When(AlsoConditionTriggered)
					 .Do((message) =>
							{
								receivedMessages.Add(message);
							}));
			}
		}

		public class LocalStateMachineStateMachineData : IStateMachineData
		{
			public virtual Guid Id { get; set; }
			public virtual string State { get; set; }
			public virtual int Version { get; set; }
			public virtual string AccountNumber { get; set; }
			public virtual bool IsVersionUpdated { get; set; }
		}

		public interface ITestMessage : IMessage
		{ }

		public class LocalSagaStateMachineMessage : IMessage
		{
			public string AccountNumber { get; set; }
		}

		public class LocalSagaStateMachineMessage2 : IMessage
		{
			public string AccountNumber { get; set; }
		}

		public class LocalSagaStateMachineMessage3 : IMessage
		{
			public string AccountNumber { get; set; }
		}

		public class LocalSagaStateMachineMessage4 : IMessage
		{
			public string AccountNumber { get; set; }
		}
	}
}