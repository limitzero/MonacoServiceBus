namespace Monaco.Configuration.Profiles
{
	/// <summary>
	/// This is the console profile for a message bus configuration. It will 
	/// configure the services and consumers in the message bus and also
	/// not purge the underlying endpoint contents when started. This configuration 
	/// is meant to have messages survive restarts of the message bus. Also, 
	/// it will not poll for timeouts like a typical server or client profile will do, 
	/// this is the reporting interface in to the systems. 
	/// </summary>
	public interface IConsoleProfile : IProfile
	{
	}
}