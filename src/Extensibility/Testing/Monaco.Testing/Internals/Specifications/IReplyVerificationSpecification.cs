namespace Monaco.Testing.Internals.Specifications
{
	public interface IReplyVerificationSpecification
	{
		void VerifyReply(IMessage message, string verification = "");
		void VerifyNonReply(IMessage message, string verification = "");
	}
}