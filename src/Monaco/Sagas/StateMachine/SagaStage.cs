namespace Monaco.Sagas.StateMachine
{
	/// <summary>
	/// Enumerates the stages within a saga.
	/// </summary>
	public enum SagaStage
	{
		Initially,
		While,
		Also
	}
}