using Castle.Core.Configuration;
using Monaco.Configuration.Bootstrapper;

namespace Monaco.Extensibility.Transports
{
	/// <summary>
	/// This is the core extensibility point for configuring and installing a custom transport onto the service bus infrastructure.
	/// </summary>
	public abstract class BaseTransportBootstrapper : BaseBootstrapper
	{
		/// <summary>
		/// Gets or sets the name of the element in the configuration to extract to begin installing the transport.
		/// </summary>
		public string ElementName { get; set; }

		public override void Configure()
		{
			this.BootUp();
		}

		private void BootUp()
		{
			this.ConfigureTransportBasedOnSettings();
			this.ConfigureTransportDependencies();
		}

		/// <summary>
		/// This is the point where the configuration section will be parsed and the custom
		/// settings are interogated for initializing the transport.
		/// </summary>
		/// <param name="configuration"></param>
		public virtual void ExtractElementSectionToConfigureTransport(Castle.Core.Configuration.IConfiguration configuration)
		{
		}

		/// <summary>
		/// This is the point where all of the dependencies that are needed for the transport are registered.
		/// </summary>
		public virtual void ConfigureTransportDependencies()
		{
		}
		
		private void ConfigureTransportBasedOnSettings()
		{
			for (int index = 0; index < this.Configuration.Children.Count; index++)
			{
				Castle.Core.Configuration.IConfiguration element = Configuration.Children[index];

				if (element == null)
					continue;

				if (this.ElementName.ToLower().Trim().Equals(element.Name.ToLower().Trim()))
				{
					this.ExtractElementSectionToConfigureTransport(element);
					break;
				}
			}
		}
	}
}