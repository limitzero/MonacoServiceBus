using System;
using Castle.MicroKernel;
using Monaco.Configuration;

namespace Monaco
{
	/// <summary>
	/// The message module is the place where actions can be taken just before a message
	/// is delivered to the message consumer from the transport and right after it has been 
	/// processed by the component. 
	/// </summary>
	public interface IMessageModule : IDisposable
	{
		/// <summary>
		/// This will allow the module to do any actions before the component receives the message.
		/// </summary>
		/// <param name="container"></param>
		/// <param name="message"></param>
		void OnMessageStartProcessing(IContainer container, object message);

		/// <summary>
		/// This will allow the module to do any actions after the component processes the message:
		/// </summary>
		/// <param name="container"></param>
		/// <param name="message"></param>
		void OnMessageEndProcessing(IContainer container, object message);
	}
}