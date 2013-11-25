using Monaco.Configuration.Profiles.Options;

namespace Monaco.Configuration.Profiles
{
	/// <summary>
	/// This is the server profile for a message bus configuration. It will 
	/// configure the services and consumers in the message bus and also
	/// not purge the underlying endpoint contents when started. This configuration 
	/// is meant to have messages survive restarts of the message bus. Also, 
	/// it will implement all of its repository look-ups for message subscriptions 
	///  via internal volatile repository implementations.
	/// </summary>
	public interface IServerProfile : IProfile, ICanPollForTasks, ICanPollForTimeouts
	{
	}
}