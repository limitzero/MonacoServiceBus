using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Monaco.Internals.Dispatcher
{
    /// <summary>
    /// Class to match a method to a message on a message consumer.
    /// </summary>
    public class MessageToMethodMapper
    {
		// TODO: make this return all methods that are assignable to the message type:
        public MethodInfo Map(object theObject, object theMessage)
        {
            MethodInfo theMethod = null;

            foreach (MethodInfo method in theObject.GetType().GetMethods())
            {
                if (theMethod != null) break;

                foreach (ParameterInfo parameterInfo in method.GetParameters())
                {
                    if (parameterInfo.ParameterType.IsInterface)
                    {
                        if (parameterInfo.ParameterType.IsAssignableFrom(theMessage.GetType()))
                        {
							theMethod = method;
							break;
                        }
                    }
                    else if (parameterInfo.ParameterType == theMessage.GetType())
                    {
                        theMethod = method;
                        break;
                    }
                   
                }
            }

        	return theMethod;
        }

        public MethodInfo Map(object theObject, object theMessage, string methodHint)
        {
            MethodInfo theMethod = null;

            var theMethods = (from method in theObject.GetType().GetMethods()
                              where method.Name.Trim().ToLower() == methodHint.Trim().ToLower()
                              select method).ToList();

            foreach (MethodInfo method in theMethods)
            {
                if(theMethod != null) break;

                foreach (ParameterInfo parameterInfo in method.GetParameters())
                {
                    if (parameterInfo.ParameterType != theMessage.GetType()) continue;
                    theMethod = method;
                    break;
                }
            }

            return theMethod;
        }
    }
}