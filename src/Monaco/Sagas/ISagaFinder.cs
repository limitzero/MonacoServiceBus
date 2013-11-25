using Monaco.Sagas.StateMachine;

namespace Monaco.Sagas
{
	/// <summary>
	/// Interface used by container to resolve all saga finders.
	/// </summary>
	public interface IFinder
	{}

	/// <summary>
	/// This will find a saga instance using the saga data for the current instance.
	/// </summary>
	/// <typeparam name="TMessage"></typeparam>
	/// <returns></returns>
	public interface ISagaFinder<TMessage> : IFinder
		where TMessage : ISagaMessage
	{
		IStateMachine Find(TMessage message);
	}

}