using System;

namespace Monaco.Tests.Messages
{
    public class AnotherTestMessage : IMessage
    {
		public Guid CorrelationId { get; set; }
    }
}