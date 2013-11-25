using System;

namespace Monaco
{
	/// <summary>
	/// Interface for all transient message and consumer instances that can be removed when needed.
	/// </summary>
	public interface IDisposableAction : IDisposable
	{
		/// <summary>
		/// Gets the flag indicating whether or not this disposed action has been cleaned-up.
		/// </summary>
		bool IsDisposed { get; }
	}
}