using System;

namespace Monaco.Bus.MessageManagement.FaultHandling
{
	[Serializable]
	public class FaultMessage<TMessage> : IFaultMessage<TMessage>
	{
		#region IFaultMessage<TMessage> Members

		public string Endpoint { get; set; }
		public TMessage Message { get; set; }
		public string Exception { get; set; }

		#endregion
	}
}