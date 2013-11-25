using System;

using Monaco.StateMachine;

namespace Monaco.Tests
{
	[Serializable]
    public class Message1 : IMessage
    {
        public int MessageId { get; set; }
		public Guid CorrelationId { get; set; }
		public string OriginatorEndpoint { get; set; }
    }

	[Serializable]
	public class Message2 : IMessage
    {
        public int MessageId { get; set; }
		public Guid CorrelationId { get; set; }
		public string OriginatorEndpoint { get; set; }
    }

	[Serializable]
	public class Message3 : IMessage
    {
        public int MessageId { get; set; }
		public Guid CorrelationId { get; set; }
		public string OriginatorEndpoint { get; set; }
    }

    [Serializable]
    public class MyTestStateMachineData : IStateMachineData
    {
		public virtual Guid Id { get; set; }
		public virtual string State { get; set; }
		public virtual int Version { get; set; }
    }

	// resulting saga from test case:
    public class MyTestStateMachine : 
		SagaStateMachine<MyTestStateMachineData>, 
		StartedBy<Message1>,
		OrchestratedBy<Message3>
    {
    	public void Consume(Message1 message)
        {
            var msg = new Message2();
			Bus.Publish(msg);
        }

        public void Consume(Message3 message)
        {
            MarkAsCompleted();
        }

    	public override void Define()
    	{
    		
    	}
    }
}