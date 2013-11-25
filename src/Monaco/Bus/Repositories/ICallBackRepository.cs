using Monaco.Bus.MessageManagement.Callbacks;

namespace Monaco.Bus.Repositories
{
	public interface ICallBackRepository
	{
		/// <summary>
		/// This will register the callback that is needed to coordinate the 
		/// request with the optional response message.
		/// </summary>
		/// <param name="callback"></param>
		void Register(ICallback callback);

		/// <summary>
		/// This will un-register the callback that is needed to coordinate the 
		/// request with the optional response message.
		/// </summary>
		/// <param name="callback"></param>
		void UnRegister(ICallback callback);

		/// <summary>
		/// This will correlate (if needed) the response back to the request message.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		ICallback Correlate(object request, object response);
	}
}