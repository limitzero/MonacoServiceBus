using System;
using Castle.MicroKernel;
using Monaco.Configuration;

namespace Monaco
{
	/// <summary>
	/// The bus module is the place where external components can 
	/// be started or stopped when the bus is started and stopped.
	/// Note: All bus modules will be hosted as singleton
	/// instances inside of the underlying container and clean-up 
	/// should be done in the Dispose() method.
	/// </summary>
	public interface IBusModule : IDisposable
	{
		/// <summary>
		/// This will allow the module to do any setup before the messages are sent on the service bus.
		/// </summary>
		/// <param name="container"></param>
		void Start(IContainer container);
	}
}