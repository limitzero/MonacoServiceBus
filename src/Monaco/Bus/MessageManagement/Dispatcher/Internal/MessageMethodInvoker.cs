using System.Reflection;

namespace Monaco.Bus.MessageManagement.Dispatcher.Internal
{
	/// <summary>
	/// Class to invoke the method on the object for the message.
	/// </summary>
	public class MessageMethodInvoker
	{
		public object Invoke(object theObject, MethodInfo theMethod, object theMessage)
		{
			return theMethod.Invoke(theObject, new[] {theMessage});
		}

		public TRETURNTYPE Invoke<TRETURNTYPE>(object theObject, MethodInfo theMethod, object theMessage)
		{
			return (TRETURNTYPE) Invoke(theObject, theMethod, theMessage);
		}
	}
}