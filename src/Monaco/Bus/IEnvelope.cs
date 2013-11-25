namespace Monaco.Bus
{
	public interface IEnvelope : IMessage
	{
		EnvelopeHeader Header { get; set; }
		EnvelopeBody Body { get; set; }
		EnvelopeFooter Footer { get; set; }
		IEnvelope Clone();
		IEnvelope Clone(object message);
	}
}