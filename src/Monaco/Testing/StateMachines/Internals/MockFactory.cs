using Castle.DynamicProxy;
using Castle.MicroKernel;
using Monaco.Testing.Internals.Interceptors.Impl;
using Monaco.Testing.Internals.Invocations;
using Monaco.Testing.Internals.Specifications.Impl;

namespace Monaco.Testing.StateMachines.Internals
{
	public class MockFactory
	{
		public static IServiceBus CreateServiceBusMock(IKernel kernel)
		{
			// create the class specificatin class to mix-in to the mock service bus:
			var mixin = new ServiceBusVerificationSpecification();

			// create the interceptor to record all calls:
			var interceptor = new ServiceBusInvocationInterceptor(kernel);

			// create the proxy as a "mock";
			var proxyGenerationOptions = new ProxyGenerationOptions();
			proxyGenerationOptions.AddMixinInstance(mixin);

			var mock = new ProxyGenerator().CreateClassProxy<MockServiceBus>(proxyGenerationOptions, interceptor) as IServiceBus;
			return mock;
		}
	}
}