using System;
using System.Collections.Generic;
using Monaco.StateMachine;

namespace Monaco.Bus.Consumers
{
	[Serializable]
	public class RequestReplyStateMachineData : IStateMachineData
	{
		public RequestReplyStateMachineData()
		{
			Requests = new List<IMessage>();
		}

		public virtual Guid Id { get; set; }
		public virtual string State { get; set; }
		public virtual int Version { get; set; }
		public virtual IMessage CurrentMessage { get; set; }
		public virtual List<IMessage> Requests { get; set; }
	}
}