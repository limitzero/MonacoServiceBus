using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Monaco.Bus.Internals.Collections
{
	/// <summary>
	/// Represents a "thread-safe" list that can be accessed by multiple consumers 
	/// (because there is not a System.Collections.Concurrent.ConcurrentList{} !!!)
	/// </summary>
	/// <typeparam name="TITEM">Item to store in the list</typeparam>
	public interface IThreadSafeList<TITEM> : IList<TITEM>
	{
		/// <summary>
		/// Gets the underlying instance of the list contents.
		/// </summary>
		ReadOnlyCollection<TITEM> Instance { get; }

		/// <summary>
		/// This will add a unique instance to the underlying list.
		/// </summary>
		/// <param name="item"></param>
		void AddUnique(TITEM item);
	}
}