using System;
using System.Linq.Expressions;
using Castle.Core.Interceptor;

namespace Monaco.Testing.Internals.Invocations
{
	public interface IStateMachineInvocation
	{
		IInvocation Invocation { get; }
		void Verify(Expression<Func<IServiceBus, object>> verify);
	}
}