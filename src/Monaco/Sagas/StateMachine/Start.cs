namespace Monaco.Sagas.StateMachine
{
	/// <summary>
	/// State that is automatically entered when defining a series of actions for an event.
	/// </summary>
	public class Start : State
	{
		public Start()
		{
			Name = this.GetType().Name;
		}
	}
}