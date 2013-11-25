namespace Monaco
{
	/// <summary>
	/// Interface marker to denote messages that should be correlated (i.e. belong 
	/// to a single conversation) together.
	/// </summary>
	/// <typeparam name="TToken"></typeparam>
	public interface CorrelatedBy<TToken>
	{
		TToken CorrelationId { get; set; }
	}
}