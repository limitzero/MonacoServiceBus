using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;
using Monaco.Configuration;

namespace Monaco.Distributor.Configuration
{
	public class MonacoDistributorConfiguration : WindsorContainer
	{
		private MonacoDistributorFacility facility;

		public IContainer Container { get; private set; }

		/// <summary>
		/// This will examine the contents of the app.config 
		/// and initialize the container from the settings.
		/// </summary>
		public MonacoDistributorConfiguration(XmlInterpreter interpreter)
			: base(interpreter)
		{
			InitializeContainer();
			RegisterFacility();
		}

		/// <summary>
		/// Initializes an instance of the container
		/// </summary>
		public MonacoDistributorConfiguration()
		{
		}

		/// <summary>
		/// This will create a new instance of the container
		/// from the standalone configuration file not tied 
		/// to the app.config (default name = monaco.config.xml).
		/// </summary>
		/// <param name="configurationFile"></param>
		public MonacoDistributorConfiguration(string configurationFile)
			: base(configurationFile)
		{
			InitializeContainer();
			RegisterFacility();
		}

		public virtual void InitializeContainer()
		{
		}

		private void RegisterFacility()
		{
			this.facility = new MonacoDistributorFacility();
			base.AddFacility(MonacoDistributorFacility.FACILITY_ID, this.facility);
			this.Container = this.facility.GetContainer();
		}
	}
}