using System;
using Monaco.StateMachine;

namespace Monaco.Storage.NHibernate.Tests
{
	public class MyStateMachineData : IStateMachineData
	{
		public virtual Guid Id { get; set; }
		public virtual string State { get; set; }
		public virtual int Version { get; set; }
		public virtual string AccountNumber { get; set; }
	}
}