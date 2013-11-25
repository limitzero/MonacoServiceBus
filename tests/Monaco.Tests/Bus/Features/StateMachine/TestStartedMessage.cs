using System;

namespace Monaco.Tests.Bus.Features.StateMachine
{
	public class TestStartedMessage : IMessage
	{
		public Guid CorrelationId { get; set; }
		public int Id { get; set; }
	}
}