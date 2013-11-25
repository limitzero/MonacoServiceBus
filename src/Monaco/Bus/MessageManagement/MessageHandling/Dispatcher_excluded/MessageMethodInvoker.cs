using System.Reflection;

namespace Monaco.Internals.Dispatcher
{
    /// <summary>
    /// Class to invoke the method on the object for the message.
    /// </summary>
    public class MessageMethodInvoker
    {
        public object Invoke(object theObject, MethodInfo theMethod, object theMessage)
        {
            return theMethod.Invoke(theObject, new object[] {theMessage});
        }

        public TRETURNTYPE Invoke<TRETURNTYPE>(object theObject, MethodInfo theMethod, object theMessage)
        {
            return (TRETURNTYPE) this.Invoke(theObject, theMethod, theMessage);
        }

    }
}