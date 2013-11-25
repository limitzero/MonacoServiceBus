namespace Monaco.Bus.MessageManagement.Callbacks
{
	public class CorrelatedCallbackDataStructure
	{
		public object CorrelationId { get; set; }
		public object Request { get; set; }
		public ICallback Callback { get; set; }
	}
}