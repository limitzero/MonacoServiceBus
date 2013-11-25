namespace Monaco.Configuration
{
	public interface IContainerConfiguration
	{
		/// <summary>
		/// Gets or sets the current local implementation of the IOC/DI container for the service bus
		/// adapted from a more specific implementation.
		/// </summary>
		IContainer Container { get; set; }
	}
}