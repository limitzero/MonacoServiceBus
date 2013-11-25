using Monaco.Bus.Messages.For.Publications;

namespace Monaco.Configuration.Profiles.Options
{
	/// <summary>
	/// Role profile for the service bus instance that will act as the central publisher 
	/// of messages to non-local endpoint instances.
	/// </summary>
	public interface ICanPublishMessages : Consumes<PublishMessage>
	{
	}
}