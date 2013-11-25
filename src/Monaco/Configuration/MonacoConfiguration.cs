using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

namespace Monaco.Configuration
{
	public class MonacoConfiguration : WindsorContainer
	{
		private MonacoFacility facility;
		private static ICanConfigureEndpoint bootedEndpointConfiguration;

		/// <summary>
		/// Gets the underlying container implementation.
		/// </summary>
		public IContainer Container { get; private set; }

		/// <summary>
		/// This will examine the contents of the app.config 
		/// and initialize the container from the settings.
		/// </summary>
		public MonacoConfiguration(XmlInterpreter interpreter)
			: base(interpreter)
		{
			InitializeContainer();
			RegisterFacility();
		}

		/// <summary>
		/// This will examine the contents of the app.config 
		/// and initialize the container from the settings.
		/// </summary>
		public MonacoConfiguration(ICanConfigureEndpoint endpoint, XmlInterpreter interpreter)
			: base(interpreter)
		{
			bootedEndpointConfiguration = endpoint;
			InitializeContainer();
			RegisterFacility();
		}

		/// <summary>
		/// Initializes an instance of the container
		/// </summary>
		public MonacoConfiguration()
		{
		}

		/// <summary>
		/// This will create a new instance of the container
		/// from the standalone configuration file not tied 
		/// to the app.config (default name = monaco.config.xml).
		/// </summary>
		/// <param name="configurationFile"></param>
		public MonacoConfiguration(string configurationFile)
			: base(configurationFile)
		{
			InitializeContainer();
			RegisterFacility();
		}

		/// <summary>
		/// This will create a new instance of the container
		/// from the standalone configuration file not tied 
		/// to the app.config (default name = monaco.config.xml).
		/// </summary>
		/// <param name="configurationFile"></param>
		public MonacoConfiguration(
			string configurationFile, 
			ICanConfigureEndpoint endpointConfiguration)
			: base(configurationFile)
		{
			bootedEndpointConfiguration = endpointConfiguration;
			InitializeContainer();
			RegisterFacility();
		}

		public virtual void InitializeContainer()
		{
		}

		/// <summary>
		/// This will tell the infrastructure to specifically use the conventions from the specified 
		/// endpoint when booting up the service bus.
		/// </summary>
		public static MonacoConfiguration BootFromEndpoint<T>()
			where T : class, ICanConfigureEndpoint, new()
		{
			bootedEndpointConfiguration = new T();
			return new MonacoConfiguration();
		}

		/// <summary>
		/// This will tell the infrastructure to specifically use the conventions from the specified 
		/// endpoint when booting up the service bus.
		/// </summary>
		/// <param name="configurationFile"></param>
		public static MonacoConfiguration BootFromEndpoint<T>(string configurationFile)
			where T : class, ICanConfigureEndpoint, new()
		{
			bootedEndpointConfiguration = new T();
			return new MonacoConfiguration(configurationFile);
		}

		/// <summary>
		/// This will tell the infrastructure to specifically use the conventions from the specified 
		/// endpoint when booting up the service bus.
		/// </summary>
		/// <param name="interpreter"></param>
		public static MonacoConfiguration BootFromEndpoint<T>(XmlInterpreter interpreter)
			where T : class, ICanConfigureEndpoint, new()
		{
			bootedEndpointConfiguration = new T();
			return new MonacoConfiguration(interpreter);
		}

		/// <summary>
		/// This will tell the infrastructure to specifically use the conventions from the specified 
		/// endpoint when booting up the service bus.
		/// </summary>
		public void SetBootEndpointConfiguration<TEndpointConfiguration>() 
			where TEndpointConfiguration :  ICanConfigureEndpoint, new()
		{
			bootedEndpointConfiguration = new TEndpointConfiguration();
		}

		public new void  Dispose()
		{
			if(this.Container != null)
			{
				this.Container.Dispose();
			}
			this.Container = null;

			base.Dispose();
		}

		private void RegisterFacility()
		{
			this.facility = new MonacoFacility(bootedEndpointConfiguration);
			base.AddFacility(MonacoFacility.FACILITY_ID, facility);
			this.Container = facility.GetContainer();
		}
	}
}