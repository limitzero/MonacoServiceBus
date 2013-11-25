using System;
using Monaco.Extensibility.Storage.StateMachines;
using Monaco.Extensibility.Storage.Subscriptions;
using Monaco.Extensibility.Storage.Timeouts;

namespace Monaco.Configuration
{
	public interface IStorageConfiguration
	{
		/// <summary>
		/// Gets the current configuration that the storage will be attached.
		/// </summary>
		IConfiguration Configuration { get;  }

		/// <summary>
		/// Gets or sets the lifecycle of the subscription repository component in the container.
		/// </summary>
		ContainerLifeCycle SubscriptionRepositoryContainerLifeCycle { get; set; }

		/// <summary>
		/// Gets or sets the type that has the concrete implementation of the <seealso cref="ISubscriptionRepository"/>
		/// </summary>
		Type SubscriptionRepository { get; set; }

		/// <summary>
		/// Gets or sets the lifecycle of the timeouts repository component in the container.
		/// </summary>
		ContainerLifeCycle TimeoutsRepositoryContainerLifeCycle { get; set; }

		/// <summary>
		/// Gets or sets the type that has the concrete implementation of the <seealso cref="ITimeoutsRepository"/>
		/// </summary>
		Type TimeoutsRepository { get; set; }

		/// <summary>
		/// Gets or sets the lifecycle of the state machine data repository component in the container.
		/// </summary>
		ContainerLifeCycle StateMachineDataRepositoryContainerLifeCycle { get; set; }

		/// <summary>
		/// Gets or sets the type that has the concrete implementation of the <seealso cref="IStateMachineDataRepository{TStateMachineData}"/>
		/// </summary>
		Type StateMachineDataRepository { get; set; }
		
	}
}