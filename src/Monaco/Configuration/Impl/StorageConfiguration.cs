using System;

namespace Monaco.Configuration.Impl
{
	public class StorageConfiguration : IStorageConfiguration
	{
		public IConfiguration Configuration { get; private set; }

		public ContainerLifeCycle SubscriptionRepositoryContainerLifeCycle { get; set; }
		public Type SubscriptionRepository { get; set; }
		public ContainerLifeCycle TimeoutsRepositoryContainerLifeCycle { get; set; }
		public Type TimeoutsRepository { get; set; }
		public ContainerLifeCycle StateMachineDataRepositoryContainerLifeCycle { get; set; }
		public Type StateMachineDataRepository { get; set; }

		public StorageConfiguration(IConfiguration configuration)
		{
			Configuration = configuration;
			SubscriptionRepositoryContainerLifeCycle = ContainerLifeCycle.Instance;
			TimeoutsRepositoryContainerLifeCycle = ContainerLifeCycle.Instance;
			StateMachineDataRepositoryContainerLifeCycle = ContainerLifeCycle.Instance;
		}
	}
}