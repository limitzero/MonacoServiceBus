using System;

namespace Monaco.Sagas
{
	/// <summary>
	/// Marker interface for container to search.
	/// </summary>
	public interface IDataFinder
	{}

	/// <summary>
	/// Contract on how to associate the persisted data for a saga to an active instance.
	/// </summary>
	/// <typeparam name="TSagaData"></typeparam>
	/// <typeparam name="TMessage"></typeparam>
	public interface ISagaDataFinder<TSagaData, TMessage> : IDataFinder
		where TSagaData : IStateMachineData
		where TMessage : ISagaMessage
	{
		/// <summary>
		/// This will return an instance of data for a saga for the given message information.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		TSagaData Find(TMessage message);
	}
}