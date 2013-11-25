using System;

namespace Monaco.Testing.MessageConsumers
{
	public interface IMessageConsumerTestScenario<TMessageConsumer>
		where TMessageConsumer : MessageConsumer
	{
		/// <summary>
		/// This will set the expectation that a message will be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToPublish<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToPublish<T>(Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToPublish<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be published by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToPublish<T>(Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToSend<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToSend<T>(Action<T> messageConstructionAction) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSend<T>() where T : IMessage;


		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on the 
		/// same endpoint as the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSend<T>(Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToSendEndpoint<T>(Uri endpoint) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToSendEndpoint<T>(Uri endpoint,
		                                                                       Action<T> messageConstructionAction)
			where T : IMessage;


		/// <summary>
		/// This will set the expectation that a message will not be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSendEndpoint<T>(Uri endpoint) where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be delivered by the service bus to a component on a 
		/// remoted endpoint from the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to send</typeparam>
		/// <param name="endpoint">Uri of the endpoint to send the message to</param>
		/// <param name="messageConstructionAction">Lambda expression to construct the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToSendEndpoint<T>(Uri endpoint,
		                                                                          Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectReplyWith<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToReplyWith<T>() where T : IMessage;

		/// <summary>
		/// This will set the expectation that a message will not be replied to (sent back) to the process 
		/// that initiated the "Send" operation by the service bus.
		/// </summary>
		/// <typeparam name="T">Type of message to publish</typeparam>
		/// <param name="messageConstructionAction">Lambda expression to create the message</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectNotToReplyWith<T>(Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will set up the test infrastructure to handle a timeout requests for processing the message sometime in the future.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="delayDuration"></param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToRequestTimeout<T>(TimeSpan delayDuration) where T : IMessage;

		/// <summary>
		/// This will set up the test infrastructure to enqueue a timeout request for processing 
		/// the message sometime in the future.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="delayDuration"></param>
		/// <param name="messageConstructionAction">Lambda expression to create the message.</param>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> ExpectToRequestTimeout<T>(TimeSpan delayDuration,
		                                                                         Action<T> messageConstructionAction)
			where T : IMessage;

		/// <summary>
		/// This will run the conditions defined and check for all expectations.
		/// </summary>
		/// <returns></returns>
		IMessageConsumerTestScenario<TMessageConsumer> VerifyAll();
	}
}