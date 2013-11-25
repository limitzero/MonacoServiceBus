namespace Monaco.Configuration.Profiles
{
	/// <summary>
	/// This is the lite profile for a message bus configuration. It will 
	/// configure the services and consumers in the message bus and also
	/// purge the underlying endpoint contents when started. This configuration 
	/// is not meant to have messages survive restarts of the message bus and will 
	/// use the default in-memory storage for sagas, timeouts and subscriptions
	/// </summary>
	public interface ILiteProfile
	{
	}
}