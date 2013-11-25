using System;

namespace Monaco.Extensions
{
	public static class ObjectExtensions
	{
		/// <summary>
		/// This will return the true underlying type of the message, especially in the case when the type is proxied.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static Type GetImplementationFromProxy(this object message)
		{
			Type result = null;

			if (message is Type)
			{
				if (((Type) message).Name.Contains("Proxy"))
				{
					// parent interface for proxied message:
					result = ((Type) message).GetInterfaces()[0];
				}
				else
				{
					result = (Type) message;
				}
			}
			else
			{
				// return current type:
				result = message.GetType();

				if (message.GetType().Name.Contains("Proxy"))
				{
					// parent interface for proxied message:
					result = message.GetType().GetInterfaces()[0];
				}
			}

			return result;
		}
	}
}