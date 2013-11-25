using System;

namespace Monaco.Tests.Bus.Features.StateMachine
{
	public class TestStartMessage : IMessage
	{
		public int Id { get; set; }
		public Guid CorrelationId { get; set; }
	}
}