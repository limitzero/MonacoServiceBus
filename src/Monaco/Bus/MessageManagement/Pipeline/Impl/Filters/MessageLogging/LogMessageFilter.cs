using Monaco.Configuration;
using Monaco.Endpoint.Factory;
using Monaco.Endpoint.Impl.Bus;

namespace Monaco.Bus.MessageManagement.Pipeline.Impl.Filters.MessageLogging
{
	/// <summary>
	/// This will forward all messages to the log endpoint.
	/// </summary>
	public class LogMessageFilter : IPipelineFilter
	{
		private readonly IContainer container;
		public string Name { get; set; }

		public LogMessageFilter(IContainer container)
		{
			this.container = container;
			this.Name = "Log Message Filter";
		}

		public void Execute(IEnvelope envelope)
		{
			IServiceBusLogEndpoint logEndpoint = null;
			IEndpointFactory endpointFactory = this.container.Resolve<IEndpointFactory>(); 

			try
			{
				logEndpoint = this.container.Resolve<IServiceBusLogEndpoint>();
				var exchange = endpointFactory.Build(logEndpoint.Endpoint);
				exchange.Transport.Send(envelope);
			}
			catch 
			{
			}
		}
	}
}