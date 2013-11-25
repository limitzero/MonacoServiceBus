namespace Monaco.Sagas.StateMachine
{
	public class SagaStateMachineDefinedTriggerCondition
	{
		public SagaStage Stage { get; private set; }
		public ISagaEventTriggerCondition Condition { get; private set; }
		public IMessage Message { get;  set; }

		public SagaStateMachineDefinedTriggerCondition(SagaStage stage, ISagaEventTriggerCondition condition)
		{
			Stage = stage;
			Condition = condition;
			Message = condition.Message;
		}
	}
}