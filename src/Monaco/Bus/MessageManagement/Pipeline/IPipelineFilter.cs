namespace Monaco.Bus.MessageManagement.Pipeline
{
	/// <summary>
	/// This is the contract for a small piece of functionality that can happen for a message while 
	/// in-flight from receive from and endpoint or being sent to an endpoint.
	/// </summary>
	public interface IPipelineFilter
	{
		string Name { get; set; }
		void Execute(IEnvelope envelope);
	}
}