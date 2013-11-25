using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Core.Interceptor;
using Monaco.Testing.Internals.Exceptions;
using Monaco.Testing.Internals.Invocations;
using Monaco.Testing.Internals.Invocations.Impl;

namespace Monaco.Testing.Internals.Specifications.Impl
{
	public class ServiceBusVerificationSpecification : IServiceBusVerificationSpecification
	{
		public ServiceBusVerificationSpecification()
		{
			Invocations = new List<BaseInvocation>();
		}

		#region IServiceBusVerificationSpecification Members

		public IList<BaseInvocation> Invocations { get; private set; }

		public void VerifyPublish<TMessage>(string verification = "") where TMessage : IMessage
		{
			if (GetInvocation<PublishInvocation, TMessage>() == null)
			{
				throw new PublishInvocationException(verification);
			}
		}

		public void VerifyPublish(IMessage message, string verification = "")
		{
			string invocationFailureMessage = string.Empty;

			if (GetInvocation<PublishInvocation>(message, out invocationFailureMessage) == null)
			{
				throw new PublishInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyNonPublish<TMessage>(string verification = "") where TMessage : IMessage
		{
			if (GetInvocation<PublishInvocation, TMessage>(true) != null)
			{
				throw new PublishInvocationException(verification);
			}
		}

		public void VerifyNonPublish(IMessage message, string verification)
		{
			string invocationFailureMessage = string.Empty;
			if (GetInvocation<PublishInvocation>(message, out invocationFailureMessage, true) != null)
			{
				throw new PublishInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifySend<TMessage>(string verification) where TMessage : IMessage
		{
			if (GetInvocation<SendInvocation, TMessage>() == null)
			{
				throw new SendInvocationException(verification);
			}
		}

		public void VerifySend(IMessage message, string verification)
		{
			string invocationFailureMessage = string.Empty;
			if (GetInvocation<SendInvocation>(message, out invocationFailureMessage) == null)
			{
				throw new SendInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyNonSend<TMessage>(string verification) where TMessage : IMessage
		{
			if (GetInvocation<SendInvocation, TMessage>(true) != null)
			{
				throw new SendInvocationException(verification);
			}
		}

		public void VerifyNonSend(IMessage message, string verification)
		{
			string invocationFailureMessage = string.Empty;
			if (GetInvocation<SendInvocation>(message, out invocationFailureMessage, true) != null)
			{
				throw new SendInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifySendToEndpoint(Uri endpoint, IMessage message, string verification)
		{
			string invocationFailureMessage = string.Empty;
			if (GetSendToEndpointInvocation(endpoint, message, out invocationFailureMessage) == null)
			{
				throw new SendInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyNonSendToEndpoint(Uri endpoint, IMessage message, string verification)
		{
			string invocationFailureMessage = string.Empty;
			if (GetSendToEndpointInvocation(endpoint, message, out invocationFailureMessage) != null)
			{
				throw new SendInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyTimeout(TimeSpan delay, IMessage message, string verification = "")
		{
			string invocationFailureMessage = string.Empty;
			if (GetTimeoutInvocation(delay, message, out invocationFailureMessage) == null)
			{
				throw new TimeoutInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyNonTimeout(TimeSpan delay, IMessage message, string verification = "")
		{
			string invocationFailureMessage = string.Empty;
			if (GetTimeoutInvocation(delay, message, out invocationFailureMessage) != null)
			{
				throw new TimeoutInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyReply(IMessage message, string verification = "")
		{
			string invocationFailureMessage = string.Empty;
			if (GetInvocation<ReplyInvocation>(message, out invocationFailureMessage) == null)
			{
				throw new ReplyInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void VerifyNonReply(IMessage message, string verification)
		{
			string invocationFailureMessage = string.Empty;
			if (GetInvocation<ReplyInvocation>(message, out invocationFailureMessage, true) != null)
			{
				throw new ReplyInvocationException(string.Concat(verification, " ", invocationFailureMessage));
			}
		}

		public void EnqueuePublishInvocation(IInvocation invocation)
		{
			Invocations.Add(new PublishInvocation(invocation));
		}

		public void EnqueueSendInvocation(IInvocation invocation)
		{
			Invocations.Add(new SendInvocation(invocation));
		}

		public void EnqueueTimeoutInvocation(IInvocation invocation)
		{
			Invocations.Add(new DelayInvocation(invocation));
		}

		public void EnqueueReplyInvocation(IInvocation invocation)
		{
			Invocations.Add(new ReplyInvocation(invocation));
		}

		#endregion

		private BaseInvocation GetTimeoutInvocation(TimeSpan duration,
		                                            IMessage message,
		                                            out string invocationFailureMessage)
		{
			invocationFailureMessage = string.Empty;
			BaseInvocation theInvocation = null;

			foreach (BaseInvocation invocation in Invocations)
			{
				if (typeof (DelayInvocation).IsAssignableFrom(invocation.GetType())
				    && InvocationReferenceEquals(invocation.Invocation.Arguments[0], duration, out invocationFailureMessage)
				    && InvocationReferenceEquals(invocation.Invocation.Arguments[1], message, out invocationFailureMessage))
				{
					theInvocation = invocation;
					break;
				}
			}

			return theInvocation;
		}

		private BaseInvocation GetSendToEndpointInvocation(Uri endpoint,
		                                                   IMessage message,
		                                                   out string invocationFailureMessage
			)
		{
			invocationFailureMessage = string.Empty;
			BaseInvocation theInvocation = null;

			foreach (BaseInvocation invocation in Invocations)
			{
				if (typeof (DelayInvocation).IsAssignableFrom(invocation.GetType())
				    && InvocationReferenceEquals(invocation.Invocation.Arguments[0], endpoint, out invocationFailureMessage)
				    && InvocationReferenceEquals(invocation.Invocation.Arguments[1], message, out invocationFailureMessage))
				{
					theInvocation = invocation;
					break;
				}
			}

			return theInvocation;
		}

		private BaseInvocation GetInvocation<TInvocation>(IMessage message,
		                                                   out string invocationFailureMessage, 
															bool useInverseExpectation = false)
			where TInvocation : BaseInvocation
		{
			invocationFailureMessage = string.Empty;
			BaseInvocation theInvocation = null;

			foreach (BaseInvocation invocation in Invocations)
			{
				if (typeof (TInvocation).IsAssignableFrom(invocation.GetType())
					 && invocation.InverseExpectationUsed == useInverseExpectation
					 && InvocationReferenceEquals(invocation.Invocation.Arguments[0],
				                              message, out invocationFailureMessage))
				{
					theInvocation = invocation;
					break;
				}
			}

			return theInvocation;
		}

		private BaseInvocation GetInvocation<TInvocation, TMessage>(
			bool useInverseExpectation = false)
			where TInvocation : BaseInvocation
			where TMessage : IMessage
		{
			BaseInvocation theInvocation = (from match in Invocations
			                                where typeof (TInvocation).IsAssignableFrom(match.GetType())
			                                      && match.Invocation.Method.IsGenericMethod
			                                      && match.Invocation.Method.GetGenericArguments()[0] == typeof (TMessage)
												  && match.InverseExpectationUsed == useInverseExpectation
			                                select match).FirstOrDefault();

			return theInvocation;
		}


		private static bool InvocationReferenceEquals(object invocationMessage,
		                                              object verifiableMessage,
		                                              out string invocationReferenceEqualsExpectationFailureMessage)
		{
			bool success = true;
			invocationReferenceEqualsExpectationFailureMessage = string.Empty;

			string messageReferenceEqualsException =
				"Message expectation failed for '{0}': Expected: {1} , Actual: {2} for property '{3}'";

			foreach (PropertyInfo invocationProperty in invocationMessage.GetType().GetProperties())
			{
				try
				{
					PropertyInfo verifiableProperty = verifiableMessage.GetType().GetProperty(invocationProperty.Name);
					object expected = verifiableProperty.GetValue(verifiableMessage, null); // expected
					object actual = invocationProperty.GetValue(invocationMessage, null); // actual

					if (expected.Equals(actual) == false)
					{
						invocationReferenceEqualsExpectationFailureMessage =
							string.Format(messageReferenceEqualsException,
							              verifiableMessage.GetType().Name,
							              expected,
							              actual,
							              invocationProperty.Name);

						success = false;

						break;
					}
				}
				catch
				{
					break;
				}
			}

			return success;
		}
	}
}