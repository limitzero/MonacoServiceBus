using System;

namespace Monaco.Testing.Internals.Specifications
{
	public interface ISendVerificationSpecification
	{
		void VerifySend<TMessage>(string verification = "")
			where TMessage : IMessage;

		void VerifySend(IMessage message, string verification = "");

		void VerifyNonSend<TMessage>(string verification = "")
			where TMessage : IMessage;

		void VerifyNonSend(IMessage message, string verification = "");

		void VerifySendToEndpoint(Uri endpoint, IMessage message, string verification = "");

		void VerifyNonSendToEndpoint(Uri endpoint, IMessage message, string verification = "");
	}
}