using System;
using System.Collections;
using System.Collections.Generic;
using Castle.Core.Interceptor;

namespace Monaco.Bus.Internals.Reflection.Internals.Impl
{
	public class InterfaceInterceptor : IInterceptor
	{
		private readonly IInterfacePersistance _interfacePersistance;

		public InterfaceInterceptor(IInterfacePersistance interfacePersistance)
		{
			_interfacePersistance = interfacePersistance;
		}

		#region IInterceptor Members

		public void Intercept(IInvocation invocation)
		{
			string propertyName = GetPropertyName(invocation);

			if (invocation.Method.Name.StartsWith("set_"))
			{
				_interfacePersistance.Store(propertyName, invocation.Arguments[0]);
			}

			if (invocation.Method.Name.StartsWith("get_"))
			{
				object value = _interfacePersistance.Retrieve(propertyName);

				if (value == null && invocation.Method.ReturnType.Name.Contains("Nullable") == false)
				{
					value = GetDefaultValue(invocation.Method.ReturnType);
				}

				invocation.ReturnValue = value;
			}
		}

		#endregion

		private static string GetPropertyName(IInvocation invocation)
		{
			return invocation.Method.Name.Replace("set_", string.Empty).Replace("get_", string.Empty);
		}

		private static object GetDefaultValue(Type currentPropertyType)
		{
			object defaultValue = null;

			if (typeof (Guid).IsAssignableFrom(currentPropertyType))
				defaultValue = Guid.Empty;

			if (typeof (DateTime).IsAssignableFrom(currentPropertyType))
				defaultValue = DateTime.MinValue;

			if (typeof (byte[]).IsAssignableFrom(currentPropertyType))
				defaultValue = new byte[] {};

			if (typeof (short).IsAssignableFrom(currentPropertyType) ||
			    typeof (int).IsAssignableFrom(currentPropertyType) ||
			    typeof (long).IsAssignableFrom(currentPropertyType) ||
			    typeof (decimal).IsAssignableFrom(currentPropertyType) ||
			    typeof (float).IsAssignableFrom(currentPropertyType) ||
			    typeof (Single).IsAssignableFrom(currentPropertyType))
				defaultValue = 0;

			if (typeof (bool).IsAssignableFrom(currentPropertyType))
				defaultValue = false;

			if (typeof (IEnumerable).IsAssignableFrom(currentPropertyType) &&
			    currentPropertyType.IsGenericType)
			{
				Type listType = typeof (IList<>).MakeGenericType(currentPropertyType.GetGenericArguments()[0]);
				object list = Activator.CreateInstance(listType);
				defaultValue = list;
			}

			return defaultValue;
		}
	}
}