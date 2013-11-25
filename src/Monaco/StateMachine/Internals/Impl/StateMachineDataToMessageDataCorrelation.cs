using System;

namespace Monaco.StateMachine.Internals.Impl
{
	public class StateMachineDataToMessageDataCorrelation
	{
		public StateMachineDataToMessageDataCorrelation(
			Type stateMachineMessage,
			string stateMachineDataPropertyName,
			string messageDataPropertyName)
		{
			StateMachineMessage = stateMachineMessage;
			StateMachineDataPropertyName = stateMachineDataPropertyName;
			MessageDataPropertyName = messageDataPropertyName;
		}

		public Type StateMachineMessage { get; private set; }
		public string StateMachineDataPropertyName { get; private set; }
		public string MessageDataPropertyName { get; private set; }

		public bool IsMatch(IStateMachineData stateMachineData, IMessage message)
		{
			object dataPropertyValue = stateMachineData.GetType()
				.GetProperty(StateMachineDataPropertyName)
				.GetValue(stateMachineData, null);

			object messagePropertyValue = message.GetType()
				.GetProperty(MessageDataPropertyName)
				.GetValue(message, null);

			bool success = messagePropertyValue.Equals(dataPropertyValue);

			return success;
		}
	}
}