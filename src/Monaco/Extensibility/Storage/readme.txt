All of the volatile storage mechanisms here can be overridden with a custom implementation 
with the following rules:

- Subscription storage must implement the ISubscriptionRepository interface
- Timeout storage must implement the ITimeoutsRepository interface
- State machine data storage must implement the BaseStateMachineDataRepository class

All of these storage mechanisms can be configured in one library with a "bootstrapper" class
included to load the storage mechanisms up when the bus is configured. Please note you can
only have one storage mechanism attached to the bus instance at start-up...

Example:

public class NHibernateStorageBootstrapper : BaseBusStorageProviderBootstrapper
{
	 public override void Configure()
	 {
		// store your references in the container as any other component...
	 }
}

By default, if no external storage is configured it uses the internal volatile storage for the items 
mentioned above.