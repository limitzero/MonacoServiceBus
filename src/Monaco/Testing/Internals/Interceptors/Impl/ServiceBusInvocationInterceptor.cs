using System;
using System.Collections.Generic;
using Castle.Core.Interceptor;
using Castle.MicroKernel;
using Monaco.Bus.Services.Timeout.Messages.Commands;
using Monaco.Extensibility.Storage.Timeouts;
using Monaco.Testing.Internals.Invocations;
using Monaco.Testing.Internals.Specifications;

namespace Monaco.Testing.Internals.Interceptors.Impl
{
	/// <summary>
	/// Interceptor to read all calls made to the mock service bus for unit testing.
	/// </summary>
	public class ServiceBusInvocationInterceptor : IInterceptor
	{
		private readonly IKernel _kernel;

		public ServiceBusInvocationInterceptor(IKernel kernel)
		{
			_kernel = kernel;
			Invocations = new List<BaseInvocation>();
		}

		public ICollection<BaseInvocation> Invocations { get; private set; }

		#region IInterceptor Members

		public void Intercept(IInvocation invocation)
		{
			var specification = invocation.InvocationTarget as IServiceBusVerificationSpecification;

			if (specification == null)
			{
				invocation.Proceed();
				return;
			}

			if (invocation.Method.Name == "Publish")
			{
				specification.EnqueuePublishInvocation(invocation);
			}

			if (invocation.Method.Name == "Send")
			{
				/* we send a scheduled timeout message when the 'RequestTimeout' method is called from a state machine */
				if(invocation.Arguments[0] != null && 
					typeof(ScheduleTimeout).IsAssignableFrom(invocation.Arguments[0].GetType()))
				{
					var timeout = invocation.Arguments[0] as ScheduleTimeout;

					if (timeout != null)
					{
						timeout.Endpoint = ((IServiceBus) invocation.InvocationTarget).Endpoint.EndpointUri.ToString();

						var repository = _kernel.Resolve<ITimeoutsRepository>();
						repository.Add(timeout);

						specification.EnqueueTimeoutInvocation(invocation);
					}
				}
				else
				{
					specification.EnqueueSendInvocation(invocation);
				}
			}

			if (invocation.Method.Name == "Reply")
			{
				specification.EnqueueReplyInvocation(invocation);
			}

			if (invocation.Method.Name == "HandleMessageLater")
			{
				object duration = invocation.Arguments[0];
				var message = invocation.Arguments[1] as IMessage;

				var timeout = new ScheduleTimeout((TimeSpan) duration, message);
				timeout.Endpoint = ((IServiceBus) invocation.InvocationTarget).Endpoint.EndpointUri.ToString();

				var repository = _kernel.Resolve<ITimeoutsRepository>();
				repository.Add(timeout);

				specification.EnqueueTimeoutInvocation(invocation);
			}

			invocation.Proceed();
		}

		#endregion
	}
}