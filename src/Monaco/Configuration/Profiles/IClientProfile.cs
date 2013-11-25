namespace Monaco.Configuration.Profiles
{
	/// <summary>
	/// This is the client  profile for a message bus configuration. It will 
	/// configure the services and consumers in the message bus and also
	/// purge the underlying endpoint contents when started. This configuration 
	/// is not meant to have messages survive restarts of the message bus.
	/// </summary>
	public interface IClientProfile
	{
	}
}