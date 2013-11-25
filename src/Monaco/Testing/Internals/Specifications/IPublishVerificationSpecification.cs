namespace Monaco.Testing.Internals.Specifications
{
	public interface IPublishVerificationSpecification
	{
		void VerifyPublish<TMessage>(string verification = "")
			where TMessage : IMessage;

		void VerifyPublish(IMessage message, string verification = "");

		void VerifyNonPublish<TMessage>(string verification = "")
			where TMessage : IMessage;

		void VerifyNonPublish(IMessage message, string verification = "");
	}
}