using System;

namespace Monaco.Bus.Roles
{
	/// <summary>
	/// Role to allow for clients participating in the BeginXX/EndXX asynchronous pattern to receive
	/// replies from the service bus.
	/// </summary>
	public interface ICanEnqueueRequests
	{
		/// <summary>
		/// This will enqueue a request to be sent using the BeginXXX/EndXXX pattern for managing
		/// the <seealso cref="IAsyncResult"/> object or to manage a semi-sychronous request reply 
		/// scenario where the calling code needs to directly get the response to a request.
		/// </summary>
		/// <returns></returns>
		IServiceAsyncRequest EnqueueRequest();
	}
}