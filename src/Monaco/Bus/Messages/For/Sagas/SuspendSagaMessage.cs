using System;
using Monaco.StateMachine;

namespace Monaco.Bus.Messages.For.Sagas
{
	[Serializable]
	public class SuspendSagaMessage : IAdminMessage
	{
		public Guid InstanceId { get; set; }
		public Type Saga { get; set; }
		public IStateMachineData Data { get; set; }
		public object State { get; set; }
	}
}