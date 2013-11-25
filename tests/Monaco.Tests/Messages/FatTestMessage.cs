using System.Collections.Generic;

namespace Monaco.Tests.Messages
{
    public class FatTestMessage : IMessage
    {
        public ICollection<IMessage> Messages { get; set; }
    }
}