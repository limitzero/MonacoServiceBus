using System;
using System.Linq.Expressions;
using Castle.Core.Interceptor;

namespace Monaco.Testing.Internals.Invocations
{
	public abstract class BaseInvocation : IStateMachineInvocation
	{
		protected BaseInvocation(IInvocation invocation)
		{
			Invocation = invocation;
		}

		#region IStateMachineInvocation Members

		public IInvocation Invocation { get; private set; }

		/// <summary>
		/// Gets or set the flag to indicate whether a "ExpectNot..." action is encountered.
		/// </summary>
		public bool InverseExpectationUsed { get; set; }

		public abstract void Verify(Expression<Func<IServiceBus, object>> verify);

		#endregion
	}
}