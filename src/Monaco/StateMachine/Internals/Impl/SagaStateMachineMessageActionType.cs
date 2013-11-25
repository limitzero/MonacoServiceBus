namespace Monaco.StateMachine.Internals.Impl
{
	public enum SagaStateMachineMessageActionType
	{
		When,
		Do,
		Publish,
		Send,
		Reply,
		Delay,
		Complete,
		Transition,
		SendToEndpoint,
		Correlate
	}
}