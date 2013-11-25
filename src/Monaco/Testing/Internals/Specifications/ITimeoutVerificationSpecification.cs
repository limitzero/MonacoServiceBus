using System;

namespace Monaco.Testing.Internals.Specifications
{
	public interface ITimeoutVerificationSpecification
	{
		void VerifyTimeout(TimeSpan delay, IMessage message, string verification = "");
		void VerifyNonTimeout(TimeSpan delay, IMessage message, string verification = "");
	}
}