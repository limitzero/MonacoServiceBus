using System;
using Castle.MicroKernel;
using Monaco.Bus.Entities;
using Monaco.Bus.MessageManagement.Serialization;
using Monaco.Bus.MessageManagement.Serialization.Impl;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.StateMachine;
using Monaco.Subscriptions;
using Monaco.Subscriptions.Impl;

namespace Monaco.Storage.NHibernate.Tests
{
    public class MyMessage : IMessage
    { }

    public class MyComponent : 
        Consumes<MyMessage>
    {
        public void Consume(MyMessage message)
        {
         
        }
    }

	public class MyStateMachine : SagaStateMachine<MyStateMachineData>
    {
    	public override void Define()
    	{
    		
    	}
    }

    public class DomainUtils
    {
        public static ISerializationProvider Serialization { get; private set; }

        static  DomainUtils()
        {
        }

        public static ISubscription CreateSubscription()
        {
            ISubscription subscription = new Subscription();
            subscription.Component = typeof(MyComponent).AssemblyQualifiedName;
            subscription.Message = typeof(MyMessage).FullName;
            subscription.IsActive = true;
            subscription.Uri = "msmq://localhost/my.message";

            return subscription;
        }

        public static SagaInstance CreateSagaThread()
        {
            Serialization = new SharpSerializationProvider();
            Serialization.AddType(typeof(MyStateMachine));
            Serialization.Initialize();

            MyStateMachine stateMachine = new MyStateMachine() {InstanceId = Guid.NewGuid(), IsCompleted = false};

            SagaInstance thread = new SagaInstance();
            thread.CreatedOn = DateTime.Now;
            thread.ModifiedOn = DateTime.Now;
            thread.Id = stateMachine.InstanceId;
            thread.SagaName = stateMachine.GetType().FullName;
            thread.Instance = Serialization.SerializeToBytes(stateMachine);

            return thread;
        }

        public static SagaDataInstance CreateSagaDataInstance()
        {
            Serialization = new DataContractSerializationProvider(null);
            Serialization.AddType(typeof(MyStateMachineData));
            Serialization.Initialize();

            MyStateMachineData sagaData = new MyStateMachineData() { Id = Guid.NewGuid()};

            SagaDataInstance thread = new SagaDataInstance();
            thread.CreatedOn = DateTime.Now;
            thread.ModifiedOn = DateTime.Now;
            thread.Id = sagaData.Id;
            thread.SagaDataName = sagaData.GetType().FullName;
            thread.Instance = Serialization.SerializeToBytes(sagaData);

            return thread;
        }

        public static Timeout CreateTimeuoutThread()
        {
            Serialization = new DataContractSerializationProvider(null);
            Serialization.AddType(typeof(MyMessage));
            Serialization.AddType(typeof(ScheduleTimeout));
            Serialization.Initialize();

            MyMessage message = new MyMessage();
            ScheduleTimeout timeout = new ScheduleTimeout(TimeSpan.FromSeconds(20), message);

            Timeout thread = new Timeout();
            thread.Invocation = timeout.At;
            thread.CreatedOn = DateTime.Now;
            thread.ModifiedOn = DateTime.Now;
            thread.Id = timeout.Id;
            thread.Message = timeout.MessageToDeliver.GetType().FullName;
            thread.Instance = Serialization.SerializeToBytes(timeout);

            return thread;
        }
    }
}
