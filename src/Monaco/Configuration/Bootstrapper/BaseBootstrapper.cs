using Castle.Core.Configuration;
using Castle.MicroKernel;

namespace Monaco.Configuration.Bootstrapper
{
	/// <summary>
	/// Base class for extending the configuration of the service bus via custom components.
	/// </summary>
	public abstract class BaseBootstrapper
	{
		protected BaseBootstrapper()
		{
			IsActive = true;
		}

		/// <summary>
		/// Gets or sets the flag to indicate whether or not the current 
		/// configuration bootstrapper instance is active for registering
		/// or configuring the environment according to user preferences
		/// (default : IsActive = true)
		/// </summary>
		public bool IsActive { get; set; }

		/// <summary>
		/// Gets or sets the instance of the current component registration kernel.
		/// </summary>
		public IContainer Container { get; set; }

		/// <summary>
		/// Gets or sets the current configuration of the service bus for extracting 
		/// custom settings for configuration.
		/// </summary>
		public Castle.Core.Configuration.IConfiguration Configuration { get; set; }

		/// <summary>
		/// This will be called and user-defined preferences can be configured here.
		/// </summary>
		public abstract void Configure();

		public bool IsMatchFor(string elementName)
		{
			return elementName.ToLower().Trim().Equals(elementName.Trim().ToLower());
		}

		public virtual void Build(Castle.Core.Configuration.IConfiguration configuration)
		{

		}
	}
}