namespace Monaco.StateMachine.Internals.Impl
{
	public class SagaStateMachineDefinedTriggerCondition
	{
		public SagaStateMachineDefinedTriggerCondition(SagaStateMachineStageType stage, ISagaEventTriggerCondition condition)
		{
			Stage = stage;
			Condition = condition;
			Message = condition.Message;
		}

		public SagaStateMachineStageType Stage { get; private set; }
		public ISagaEventTriggerCondition Condition { get; private set; }
		public IMessage Message { get; set; }
	}
}