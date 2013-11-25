using System;

namespace Monaco.Bus.Internals
{
	public interface IStartable : IDisposable
	{
		bool IsRunning { get; }
		void Start();
		void Stop();
	}
}