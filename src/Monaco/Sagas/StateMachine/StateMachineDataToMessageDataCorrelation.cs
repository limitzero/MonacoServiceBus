using System;

namespace Monaco.Sagas.StateMachine
{
	public class StateMachineDataToMessageDataCorrelation
	{
		public Type StateMachineMessage { get; private set; }
		public string StateMachineDataPropertyName { get; private set; }
		public string MessageDataPropertyName { get; private set; }

		public StateMachineDataToMessageDataCorrelation(
			Type stateMachineMessage, 
			string stateMachineDataPropertyName, 
			string messageDataPropertyName)
		{
			StateMachineMessage = stateMachineMessage;
			StateMachineDataPropertyName = stateMachineDataPropertyName;
			MessageDataPropertyName = messageDataPropertyName;
		}

		public bool IsMatch(IStateMachineData stateMachineData, IMessage message)
		{
			var dataPropertyValue = stateMachineData.GetType()
									.GetProperty(this.StateMachineDataPropertyName)
									.GetValue(stateMachineData, null);

			var messagePropertyValue = message.GetType()
					.GetProperty(this.MessageDataPropertyName)
					.GetValue(message, null);

			bool success = messagePropertyValue.Equals(dataPropertyValue);

			return success;
		}
	}
}