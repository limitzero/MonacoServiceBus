using System.Collections.Generic;
using Castle.Core.Interceptor;
using Monaco.Testing.Internals.Invocations;

namespace Monaco.Testing.Internals.Specifications
{
	/// <summary>
	/// This is the verification specification that will be "mixed-in" 
	/// to the mock service bus class for verifying service bus actions
	/// in unit testing of state machines.
	/// </summary>
	public interface IServiceBusVerificationSpecification :
		IPublishVerificationSpecification,
		ISendVerificationSpecification,
		ITimeoutVerificationSpecification,
		IReplyVerificationSpecification
	{
		/// <summary>
		/// Gets or sets the current set of invocations on the mock service bus
		/// </summary>
		IList<BaseInvocation> Invocations { get; }

		void EnqueuePublishInvocation(IInvocation invocation);
		void EnqueueSendInvocation(IInvocation invocation);
		void EnqueueTimeoutInvocation(IInvocation invocation);
		void EnqueueReplyInvocation(IInvocation invocation);
	}
}