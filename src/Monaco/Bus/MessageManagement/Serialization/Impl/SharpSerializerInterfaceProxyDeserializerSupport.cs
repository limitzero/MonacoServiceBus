using System;
using System.Linq;
using Castle.DynamicProxy;
using Monaco.Bus.Internals.Reflection.Internals.Impl;
using Polenter.Serialization.Advanced.Proxy;

namespace Monaco.Bus.MessageManagement.Serialization.Impl
{
	public class SharpSerializerInterfaceProxyDeserializerSupport :
		BaseDeserializerTypeResolver
	{
		public override Type ResolveToSpecifiedType(string typeName)
		{
			Type theType = null;

			if (typeName == null) return theType;

			string type = typeName.Split(new[] {','})[0].Trim();

			if (typeName.StartsWith("Castle.Proxies"))
			{
				// remove reference to Castle generated proxies:
				type = type
					.Replace("Castle.Proxies.", string.Empty)
					.Replace("Proxy", string.Empty);

				Type interfaceType = (from asm in AppDomain.CurrentDomain.GetAssemblies()
				                      from aType in asm.GetTypes()
				                      where aType.Name.EndsWith(type)
				                      select aType).FirstOrDefault();

				var storage = new InterfacePersistance();
				var interceptor = new InterfaceInterceptor(storage);

				// proxy of interface created here for use (no need to go to container for this):
				var proxyGenerator = new ProxyGenerator();
				theType = proxyGenerator.CreateInterfaceProxyWithoutTarget(interfaceType, interceptor).GetType();
			}

			return theType;
		}

		public override bool CanCreateFor(Type type)
		{
			return type.FullName.Contains("Castle.Proxies");
		}

		public override object CreateType(Type type)
		{
			var storage = new InterfacePersistance();
			var interceptor = new InterfaceInterceptor(storage);

			Type theInterface = type;

			if (type.GetInterfaces().Length > 0)
			{
				theInterface = type.GetInterfaces()[0];
			}

			// proxy of interface created here for use in  populating object:
			var proxyGenerator = new ProxyGenerator();
			object proxy = proxyGenerator.CreateInterfaceProxyWithoutTarget(theInterface, interceptor);

			return proxy;
		}
	}
}