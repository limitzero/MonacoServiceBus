using System;
using System.Linq.Expressions;
using Castle.Core.Interceptor;

namespace Monaco.Testing.Internals.Invocations.Impl
{
	public class PublishInvocation : BaseInvocation
	{
		public PublishInvocation(IInvocation invocation) : base(invocation)
		{
		}

		public override void Verify(Expression<Func<IServiceBus, object>> verify)
		{
		}
	}
}