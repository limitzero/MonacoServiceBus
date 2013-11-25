using System.Collections.Generic;

namespace Monaco.Bus.MessageManagement.Pipeline
{
	/// <summary>
	/// This is the contract for the internal pipeline for receiving messages from and endpoint 
	/// and processing messages to an endpoint.
	/// </summary>
	public interface IPipeline
	{
		/// <summary>
		/// Gets or sets the collection of filters to be executed prior to the message being relayed to the endpoint.
		/// </summary>
		IEnumerable<IPipelineFilter> PreSendFilters { get; }

		/// <summary>
		/// Gets or sets the collection of filters to be executed just after the message is delivered to the endpoint.
		/// </summary>
		IEnumerable<IPipelineFilter> PostSendFilters { get; }

		/// <summary>
		/// Gets or sets the collection of filters to be executed just after the message has been recevied from the endpoint
		/// but before the message is delivered to the components for processing.
		/// </summary>
		IEnumerable<IPipelineFilter> PreReceiveFilters { get; }

		/// <summary>
		/// Gets or sets the collection of filters to be executed just after the message has been recevied from the endpoint
		/// and processed by the components.
		/// </summary>
		IEnumerable<IPipelineFilter> PostReceiveFilters { get; }

		void RegisterPreSendFilter(IPipelineFilter filter);
		void RegisterPostSendFilter(IPipelineFilter filter);

		void RegisterPreReceiveFilter(IPipelineFilter filter);
		void RegisterPostReceiveFilter(IPipelineFilter filter);

		/// <summary>
		/// This will execute the current pipeline for processing messages.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="bus"></param>
		/// <param name="envelope"></param>
		void Execute(PipelineDirection direction, IServiceBus bus, IEnvelope envelope);
	}
}