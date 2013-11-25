using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;
using Monaco.Bus.Messages;
using Monaco.Configuration;
using Monaco.Extensibility.Logging;
using Monaco.Extensions;

namespace Monaco.Bus.MessageManagement.Pipeline.Impl.Filters.MesssageModules
{
	public class EndMessageModulesFilter : IPipelineFilter
	{
		private readonly IContainer container;

		public EndMessageModulesFilter(IContainer container)
		{
			this.container = container;
		}

		#region IPipelineFilter Members

		public string Name { get; set; }

		public void Execute(IEnvelope envelope)
		{
			if (typeof (IAdminMessage).IsAssignableFrom(envelope.Body.Payload.GetType())) return;

			IEnumerable<IMessageModule> modules = this.container.ResolveAll<IMessageModule>().Distinct();

			if (modules != null && modules.Count() == 0) return;

			foreach (IMessageModule module in modules)
			foreach (var message in envelope.Body.Payload)
			{
				try
				{
					envelope.Header.RecordStage(module, message, "End Message Module - " + module.GetType().Name);
					module.OnMessageEndProcessing(this.container, message);
				}
				catch (Exception startModuleException)
				{
					string msg =
						string.Format(
							"An error has occurred while attempting to execute the message module '{0}' for the end processing action, " +
							" it will be skipped as a result. Reason: {1}",
							module.GetType().FullName, startModuleException);
					this.container.Resolve<ILogger>().LogInfoMessage(msg);

					continue;
				}
			}
		}

		#endregion
	}
}