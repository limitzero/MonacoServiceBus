using System;

namespace Monaco.Sagas.StateMachine
{
	/// <summary>
	/// Actions that can be taken on a saga for a given message.
	/// </summary>
	public class MessageAction
	{
		/// <summary>
		/// Gets the type of action to be taken on the message by the bus for the saga.
		/// </summary>
		public SagaMessageActionType ActionType { get;  set; }

		/// <summary>
		/// Gets the message that will be acted upon in the saga.
		/// </summary>
		public object Message { get;  set; }

		/// <summary>
		/// Gets the executable action to be taken on the message by the bus for the saga.
		/// </summary>
		public Action<IMessage> Action { get; private set; }

		/// <summary>
		/// Gets or sets the state of the current message action.
		/// </summary>
		public State State { get; set; }

		/// <summary>
		/// Gets or sets the endpoint location where one or more messges will be sent.
		/// </summary>
		public string Endpoint { get; set; }

		/// <summary>
		/// Gets or sets the delay interval for a message to be submitted.
		/// </summary>
		public TimeSpan Delay { get; set; }

		/// <summary>
		/// Gets or sets any documentation notes for the action.
		/// </summary>
		public string Note { get; set; }

		public MessageAction()
		{
		}
			
		public MessageAction(SagaMessageActionType actionType, object message, 
			Action<IMessage> action, bool canExecuteAction = true)
		{
			ActionType = actionType;
			Message = message;

			if(canExecuteAction)
				Action = action;
		}		
	}

	public class CorrelateMessageAction<TMessage> : 
		MessageAction
	{
		/// <summary>
		/// Gets the function for performing the message correlation.
		/// </summary>
		public Func<TMessage, bool> CorrelateAction { get; private set; }

		public CorrelateMessageAction(SagaMessageActionType actionType, TMessage message, 
			Func<TMessage, bool> action, bool canExecuteAction = true)
		{
			ActionType = actionType;
			Message = message;

			if (canExecuteAction)
				CorrelateAction = action;
		}

	}

}