using System;
using System.Linq.Expressions;
using Monaco.StateMachine;

namespace Monaco.Testing.StateMachines
{
	public interface IStateMachineTestScenario<TStateMachine> 
		where TStateMachine : SagaStateMachine
	{
		/// <summary>
		/// This will set the expectation that a message will be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToPublish<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToPublish<T>(Action<T> messageConstructionAction) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToPublish<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToPublish<T>(Action<T> messageConstructionAction) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToSend<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToSend<T>(Action<T> messageConstructionAction) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToSend<T>() where T : IMessage;


		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToSend<T>(Action<T> messageConstructionAction) where T : IMessage;


		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToSendEndpoint<T>(Uri endpoint) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToSendEndpoint<T>(Uri endpoint, Action<T> messageConstructionAction)
			where T : IMessage;


		/// <summary>
		/// This will set the expectation that a message will not be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToSendEndpoint<T>(Uri endpoint) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToSendEndpoint<T>(Uri endpoint, Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectReplyWith<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectReplyWith<T>(Action<T> messageConstructionAction) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToReplyWith<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set up the test infrastructure to handle a timeout requests for processing the message sometime in the future.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="delayDuration"></param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToRequestTimeout<T>(TimeSpan delayDuration) where T : IMessage;

		/// <summary>
		/// This will set up the test infrastructure to enqueue a timeout request for processing 
		/// the message sometime in the future.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="delayDuration"></param>
		/// <param name="messageConstructionAction">Lambda expression to create the message.</param>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToRequestTimeout<T>(TimeSpan delayDuration,
		                                                                   Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will check the expectation that the state machine will transition to a 
		/// defined state on the state machine for the current processing scenario.
		/// </summary>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToTransitionToState(
			Expression<Func<TStateMachine, State>> expectedState);

		/// <summary>
		/// This will check the expectation that the state machine will mark is 
		/// processing as 'completed' for the current processing scenario.
		/// </summary>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectToComplete();

		/// <summary>
		/// This will check the expectation that the state machine will not mark is 
		/// processing as 'completed' for the current processing scenario.
		/// </summary>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> ExpectNotToComplete();

		/// <summary>
		/// This will extract the current localized data of the state machine for inspection and evaluate 
		/// the instance data to a boolean condition for in-flight verification.
		/// </summary>
		/// <typeparam name="TStateMachineData">Type representing the state machine data</typeparam>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> SetAssertOn<TStateMachineData>(
			Expression<Func<TStateMachineData, bool>> expectation,
			string expectationMessage = "")
			where TStateMachineData : class, IStateMachineData;

		/// <summary>
		/// This will run the conditions defined and check for all expectations.
		/// </summary>
		/// <returns></returns>
		IStateMachineTestScenario<TStateMachine> Verify();

		IStateMachineTestScenario<TStateMachine> ExpectNotToDelay<T>(TimeSpan delayDuration) where T : IMessage;

		IStateMachineTestScenario<TStateMachine> ExpectNotToDelay<T>(TimeSpan delayDuration,
		                                                             Action<T> messageConstructionAction) where T : IMessage;
	}
}