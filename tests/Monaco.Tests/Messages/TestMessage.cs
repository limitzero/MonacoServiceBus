using System;


namespace Monaco.Tests.Messages
{
    public class TestMessage : IMessage
    {
		public Guid CorrelationId { get; set; }
    }
}