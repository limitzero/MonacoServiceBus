namespace Monaco.StateMachine.Internals.Impl
{
	/// <summary>
	/// Enumerates the stages within a saga state machine.
	/// </summary>
	public enum SagaStateMachineStageType
	{
		Initially,
		While,
		Also
	}
}