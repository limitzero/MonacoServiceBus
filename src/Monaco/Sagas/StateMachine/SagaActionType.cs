namespace Monaco.Sagas.StateMachine
{
	public enum SagaMessageActionType
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