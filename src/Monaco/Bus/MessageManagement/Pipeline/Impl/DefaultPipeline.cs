using System;
using Castle.MicroKernel;
using Monaco.Bus.MessageManagement.Pipeline.Impl.Filters.MessageLogging;
using Monaco.Configuration;

namespace Monaco.Bus.MessageManagement.Pipeline.Impl
{
	public class DefaultPipeline : BasePipeline
	{
		private readonly IContainer container;

		public DefaultPipeline(IContainer container) : base(container)
		{
			this.container = container;
			this.Initialize();
		}

		public void RegisterPostSendFilters(params Type[] postSendFilters)
		{
			foreach (Type postSendFilter in postSendFilters)
			{
				IPipelineFilter filter = RegisterAndResolveFilter(postSendFilter);
				if (filter != null) base.RegisterPostSendFilter(filter);
			}
		}

		public void RegisterPreSendFilters(params Type[] preSendFilters)
		{
			foreach (Type preSendFilter in preSendFilters)
			{
				IPipelineFilter filter = RegisterAndResolveFilter(preSendFilter);
				if (filter != null) base.RegisterPreSendFilter(filter);
			}
		}

		public void RegisterPostReceiveFilters(params Type[] postReceiveFilters)
		{
			foreach (Type postReceiveFilter in postReceiveFilters)
			{
				IPipelineFilter filter = RegisterAndResolveFilter(postReceiveFilter);
				if (filter != null) base.RegisterPostReceiveFilter(filter);
			}
		}

		private IPipelineFilter RegisterAndResolveFilter(Type pipelineFilter)
		{
			if (typeof (IPipelineFilter).IsAssignableFrom(pipelineFilter) == false)
				throw new InvalidOperationException(string.Format(
					"The filter '{0}' for the pipeline must be derivable from '{1}'.",
					pipelineFilter.FullName,
					typeof (IPipelineFilter).FullName));

			IPipelineFilter filter = null; 

			try
			{
				// resolve the filter:
				filter = container.Resolve(pipelineFilter) as IPipelineFilter;
			}
			catch 
			{
				// register the filter (if not found):
				this.container.Register(pipelineFilter);
				filter = container.Resolve(pipelineFilter) as IPipelineFilter;
			}

			return filter;
		}

		private void Initialize()
		{
			// add the logging filters (if the logging option is configured):
			this.RegisterPostSendFilters(typeof(LogMessageFilter));
			this.RegisterPostReceiveFilters(typeof(LogMessageFilter));
		}
	}
}